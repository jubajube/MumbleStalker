#include <condition_variable>
#include <Ice/Ice.h>
#include <list>
#include <Murmur.h>
#include <mutex>
#include <signal.h>
#include <sstream>
#include <stdio.h>
#include <stdlib.h>
#include <string>

struct User {
    std::string name;
    int userid;

    User(const Murmur::User& serverUserInformation) {
        name = serverUserInformation.name;
        userid = (int)serverUserInformation.userid;
    }

    bool operator==(const User& other) {
        if (name != other.name) {
            return false;
        }
        if (userid != other.userid) {
            return false;
        }
        return true;
    }
};

struct ProgramState {
    Ice::CommunicatorPtr iceCommunicator;
    Ice::ConnectionPtr connection;
    Murmur::ServerPrx server;
    Murmur::ServerCallbackPrx serverCallback;
    bool serverCallbackAdded = false;
    std::list< User > users;
    bool quit = false;
    std::mutex mutex;
    std::condition_variable wakeCondition;

    static ProgramState* signalContext;

    void HandleSignal(int signal) {
        if (signal == SIGINT) {
            quit = true;
            wakeCondition.notify_one();
        }
    }

    void AddUser(User&& newUser) {
        std::unique_lock< std::mutex > lock(mutex);
        for (auto it = users.begin(); it != users.end(); ++it) {
            if (*it == newUser) {
                return;
            }
        }
        users.emplace_back(newUser);
    }

    void RemoveUser(const User& oldUser) {
        std::unique_lock< std::mutex > lock(mutex);
        for (auto it = users.begin(); it != users.end(); ++it) {
            if (*it == oldUser) {
                (void)users.erase(it);
                break;
            }
        }
    }

    ~ProgramState() {
        if (signalContext == this) {
            signalContext = nullptr;
        }
        if (server && serverCallback && serverCallbackAdded) {
            server->removeCallback(serverCallback);
        }
        if (iceCommunicator) {
            try {
                iceCommunicator->destroy();
            } catch(...) {
            }
        }
    }
};
ProgramState* ProgramState::signalContext = nullptr;

static void ProgramSignalHandler(int signal) {
    if (ProgramState::signalContext != nullptr) {
        ProgramState::signalContext->HandleSignal(signal);
    }
}

class ServerCallback: public Murmur::ServerCallback {
    // Public methods
public:
    explicit ServerCallback(ProgramState& state)
        : _state(state)
    {
    }

    // Murmur::ServerCallback
public:
    virtual void userConnected(const ::Murmur::User& user, const ::Ice::Current&) override {
        _state.AddUser(user);
        printf("User connected: %s\n", user.name.c_str());
    }

    virtual void userDisconnected(const ::Murmur::User& user, const ::Ice::Current&) override {
        _state.RemoveUser(user);
        printf("User disconnected: %s\n", user.name.c_str());
    }

    virtual void userStateChanged(const ::Murmur::User& user, const ::Ice::Current&) override {
        printf("User changed: %s\n", user.name.c_str());
    }

    virtual void userTextMessage(const ::Murmur::User& user, const ::Murmur::TextMessage& message, const ::Ice::Current&) override {
        printf("User %s send text message: %s\n", user.name.c_str(), message.text.c_str());
    }

    virtual void channelCreated(const ::Murmur::Channel& channel, const ::Ice::Current&) override {
        printf("Channel created: %s\n", channel.name.c_str());
    }

    virtual void channelRemoved(const ::Murmur::Channel& channel, const ::Ice::Current&) override {
        printf("Channel removed: %s\n", channel.name.c_str());
    }

    virtual void channelStateChanged(const ::Murmur::Channel& channel, const ::Ice::Current&) override {
        printf("Channel changed: %s\n", channel.name.c_str());
    }

    // Private properties
private:
    ProgramState& _state;
};

std::string FormClientEndpoint(const std::string& localAddress) {
    std::ostringstream buf;
    buf << "tcp -h " << localAddress << " -t 1000";
    return buf.str();
}

int main(int argc, char* argv[]) {
    ProgramState state;
    ProgramState::signalContext = &state;
    (void)signal(SIGINT, ProgramSignalHandler);
    try {
        auto properties = Ice::createProperties(argc, argv);
        properties->setProperty("Ice.Default.EncodingVersion", "1.0");
        Ice::InitializationData initializationData;
        initializationData.properties = properties;
        state.iceCommunicator = Ice::initialize(initializationData);
    } catch (const Ice::Exception& e) {
        fprintf(stderr, "error: unable to initialize ICE: %s\n", e.what());
        return EXIT_FAILURE;
    }
    try {
        auto connectionProxy = state.iceCommunicator->stringToProxy("Meta:tcp -h 192.168.1.201 -p 6502 -t 1000");
        state.connection = connectionProxy->ice_getConnection();
        auto localAddress = Ice::TCPConnectionInfoPtr::dynamicCast(state.connection->getInfo())->localAddress;
        auto clientEndpoint = FormClientEndpoint(localAddress);
        auto metaProxy = Murmur::MetaPrx::checkedCast(connectionProxy);
        for (auto serverProxy: metaProxy->getBootedServers()) {
            if (!state.server) {
                state.server = serverProxy;
            }
        }
        if (!state.server) {
            fprintf(stderr, "No server!\n");
            return EXIT_FAILURE;
        }
        printf("Server: %d\n", (int)state.server->id());

        auto servant = new ServerCallback(state);
        auto adapter = state.iceCommunicator->createObjectAdapterWithEndpoints("", clientEndpoint);
        auto servantProxy = adapter->addWithUUID(servant);
        state.serverCallback = Murmur::ServerCallbackPrx::checkedCast(servantProxy);
        adapter->activate();

        // TODO: Allow user to provide Ice secret
        Ice::Context context;
        context["secret"] = "";

        state.server->ice_getConnection()->setAdapter(adapter);
        state.server->addCallback(state.serverCallback, context);
        state.serverCallbackAdded = true;
        auto users = state.server->getUsers();
        for (auto user: users) {
            printf("User: %s(%d)\n", user.second.name.c_str(), (int)user.second.userid);
            state.AddUser(user.second);
        }

        std::unique_lock< std::mutex > lock(state.mutex);
        while (!state.quit) {
            state.wakeCondition.wait(lock);
            printf("Users: [");
            bool first = true;
            for (auto user: state.users) {
                if (!first) {
                    printf(" ");
                }
                first = false;
                printf("%s(%d)", user.name.c_str(), (int)user.userid);
            }
            printf("]\n");
        }

        printf("Done.\n");
    } catch (const Ice::Exception& e) {
        fprintf(stderr, "error: %s\n", e.what());
        return EXIT_FAILURE;
    }
    return EXIT_SUCCESS;
}
