#include <condition_variable>
#include <errno.h>
#include <fcntl.h>
#include <Ice/Ice.h>
#include <libdaemon/dfork.h>
#include <libdaemon/dsignal.h>
#include <libdaemon/dlog.h>
#include <libdaemon/dpid.h>
#include <list>
#include <Murmur.h>
#include <mutex>
#include <signal.h>
#include <sstream>
#include <stdint.h>
#include <stdio.h>
#include <stdlib.h>
#include <string>
#include <string.h>
#include <sys/mman.h>
#include <sys/select.h>
#include <sys/stat.h>
#include <sys/time.h>
#include <sys/types.h>
#include <sys/unistd.h>
#include <unistd.h>

static const uint16_t FONT[96] = {
    // 0x20 - 0x2F
    0x0000, // ' '
    0x1509, // '!'
    0x0202, // '"'
    0x12C6, // '#'
    0x12ED, // '$'
    0x0C24, // '%'
    0x2559, // '&'
    0x0400, // '\''
    0x2400, // '('
    0x0900, // ')'
    0x3FC0, // '*'
    0x12C0, // '+'
    0x0800, // ','
    0x00C0, // '-'
    0x4000, // '.'
    0x0C00, // '/'

    // 0x30 - 0x3F
    0x0C3F, // '0'
    0x0406, // '1'
    0x00DB, // '2'
    0x00CF, // '3'
    0x00E6, // '4'
    0x2069, // '5'
    0x00FD, // '6'
    0x14C1, // '7'
    0x00FF, // '8'
    0x00EF, // '9'
    0x1200, // ':'
    0x0A00, // ';'
    0x0C08, // '<'
    0x00C8, // '='
    0x2108, // '>'
    0x10A3, // '?'

    // 0x40 - 0x4F
    0x02BB, // '@'
    0x00F7, // 'A'
    0x128F, // 'B'
    0x0039, // 'C'
    0x120F, // 'D'
    0x00F9, // 'E'
    0x00F1, // 'F'
    0x00BD, // 'G'
    0x00F6, // 'H'
    0x1209, // 'I'
    0x001E, // 'J'
    0x2470, // 'K'
    0x0038, // 'L'
    0x0536, // 'M'
    0x2136, // 'N'
    0x003F, // 'O'

    // 0x50 - 0x5F
    0x00F3, // 'P'
    0x203F, // 'Q'
    0x20F3, // 'R'
    0x00ED, // 'S'
    0x1201, // 'T'
    0x003E, // 'U'
    0x0C30, // 'V'
    0x2836, // 'W'
    0x2D00, // 'X'
    0x1500, // 'Y'
    0x0C09, // 'Z'
    0x0949, // '['
    0x2100, // '\'
    0x2489, // ']'
    0x0402, // '^'
    0x0008, // '_'

    // 0x60 - 0x6F
    0x0100, // '`'
    0x00DF, // 'a'
    0x00FC, // 'b'
    0x00D8, // 'c'
    0x00DE, // 'd'
    0x00FB, // 'e'
    0x14C0, // 'f'
    0x018F, // 'g'
    0x00F4, // 'h'
    0x1000, // 'i'
    0x000C, // 'j'
    0x3600, // 'k'
    0x1208, // 'l'
    0x10D4, // 'm'
    0x2050, // 'n'
    0x00DC, // 'o'

    // 0x70 - 0x7F
    0x0471, // 'p'
    0x0187, // 'q'
    0x00D0, // 'r'
    0x2088, // 's'
    0x0078, // 't'
    0x001C, // 'u'
    0x0810, // 'v'
    0x2814, // 'w'
    0xADC0, // 'x'
    0x028E, // 'y'
    0x0848, // 'z'
    0x2480, // '{'
    0x0030, // '|'
    0x0940, // '}'
    0x0001, // '~'
    0x3FFF  // DEL
};

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
    std::string clientEndpoint;
    Murmur::ServerPrx server;
    Murmur::ServerCallbackPrx serverCallback;
    bool serverCallbackAdded = false;
    std::list< User > users;
    bool quit = false;
    std::mutex mutex;
    uint16_t* framebuffer = (uint16_t*)MAP_FAILED;

    void FormClientEndpoint(const std::string& localAddress) {
        std::ostringstream buf;
        buf << "tcp -h " << localAddress << " -t 1000";
        clientEndpoint = buf.str();
    }

    void SetCounter(int count) {
        if (framebuffer != (uint16_t*)MAP_FAILED) {
            char digits[5];
            sprintf(digits, "%4d", count);
            for (int i = 0; i < 4; ++i) {
                framebuffer[i] = FONT[digits[i] - 32];
            }
        }
        FILE* digits = fopen("/sys/devices/platform/soc/soc:gpio-segled/panel0/digits", "w");
        if (digits != NULL) {
            if (count >= 0) {
                (void)fprintf(digits, "%d", count);
            } else {
                (void)fprintf(digits, " ");
            }
            (void)fclose(digits);
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
        SetCounter((int)users.size());
    }

    void RemoveUser(const User& oldUser) {
        std::unique_lock< std::mutex > lock(mutex);
        for (auto it = users.begin(); it != users.end(); ++it) {
            if (*it == oldUser) {
                (void)users.erase(it);
                break;
            }
        }
        SetCounter((int)users.size());
    }

    ProgramState() {
        int fp = open("/dev/fb1", O_RDWR);
        if (fp != 0) {
            framebuffer = (uint16_t*)mmap(NULL, 8, PROT_READ | PROT_WRITE, MAP_SHARED, fp, 0);
            (void)close(fp);
        }
        SetCounter(0);
    }

    ~ProgramState() {
        if (server && serverCallback && serverCallbackAdded) {
            server->removeCallback(serverCallback);
        }
        if (iceCommunicator) {
            try {
                iceCommunicator->destroy();
            } catch(...) {
            }
        }
        SetCounter(-1);
    }
};

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
        daemon_log(LOG_INFO, "User connected: %s", user.name.c_str());
    }

    virtual void userDisconnected(const ::Murmur::User& user, const ::Ice::Current&) override {
        _state.RemoveUser(user);
        daemon_log(LOG_INFO, "User disconnected: %s", user.name.c_str());
    }

    virtual void userStateChanged(const ::Murmur::User& user, const ::Ice::Current&) override {
        daemon_log(LOG_INFO, "User changed: %s", user.name.c_str());
    }

    virtual void userTextMessage(const ::Murmur::User& user, const ::Murmur::TextMessage& message, const ::Ice::Current&) override {
        daemon_log(LOG_INFO, "User %s send text message: %s", user.name.c_str(), message.text.c_str());
    }

    virtual void channelCreated(const ::Murmur::Channel& channel, const ::Ice::Current&) override {
        daemon_log(LOG_INFO, "Channel created: %s", channel.name.c_str());
    }

    virtual void channelRemoved(const ::Murmur::Channel& channel, const ::Ice::Current&) override {
        daemon_log(LOG_INFO, "Channel removed: %s", channel.name.c_str());
    }

    virtual void channelStateChanged(const ::Murmur::Channel& channel, const ::Ice::Current&) override {
        daemon_log(LOG_INFO, "Channel changed: %s", channel.name.c_str());
    }

    // Private properties
private:
    ProgramState& _state;
};

int main(int argc, char* argv[]) {
    if (daemon_reset_sigs(-1) < 0) {
        daemon_log(LOG_ERR, "Failed to reset all signal handlers: %s", strerror(errno));
        return EXIT_FAILURE;
    }
    if (daemon_unblock_sigs(-1) < 0) {
        daemon_log(LOG_ERR, "Failed to unblock all signals: %s", strerror(errno));
        return EXIT_FAILURE;
    }
    daemon_pid_file_ident = daemon_log_ident = daemon_ident_from_argv0(argv[0]);
    if (
        (argc >= 2)
        && (!strcmp(argv[1], "-k"))
    ) {
        const int ret = daemon_pid_file_kill_wait(SIGTERM, 5);
        if (ret < 0) {
            daemon_log(LOG_WARNING, "Failed to kill daemon: %s", strerror(errno));
        }
        return (ret < 0) ? EXIT_FAILURE : EXIT_SUCCESS;
    }
    pid_t pid = daemon_pid_file_is_running();
    if (pid >= 0) {
        daemon_log(LOG_ERR, "Daemon already running on PID file %u", pid);
        return EXIT_FAILURE;
    }
    if (daemon_retval_init() < 0) {
        daemon_log(LOG_ERR, "Failed to create pipe");
        return EXIT_FAILURE;
    }
    pid = daemon_fork();
    if (pid < 0) {
        return EXIT_FAILURE;
    } else if (pid > 0) {
        const int ret = daemon_retval_wait(5);
        if (ret < 0) {
            daemon_log(LOG_ERR, "Could not receive return value from daemon process: %s", strerror(errno));
            return EXIT_FAILURE;
        }
        if (ret == EXIT_SUCCESS) {
            daemon_log(LOG_INFO, "Daemon started successfully");
        } else {
            daemon_log(LOG_ERR, "Daemon failed to start successfully");
        }
        return ret;
    }

    ProgramState state;

    if (daemon_close_all(-1) < 0) {
        daemon_log(LOG_ERR, "Failed to close all file descriptors: %s", strerror(errno));
        daemon_retval_send(EXIT_FAILURE);
        goto done;
    }
    if (daemon_pid_file_create() < 0) {
        daemon_log(LOG_ERR, "Could not create PID file: %s", strerror(errno));
        daemon_retval_send(EXIT_FAILURE);
        goto done;
    }
    if (daemon_signal_init(SIGINT, SIGTERM, SIGQUIT, SIGHUP, 0) < 0) {
        daemon_log(LOG_ERR, "Could not register signal handlers: %s", strerror(errno));
        daemon_retval_send(EXIT_FAILURE);
        goto done;
    }
    try {
        auto properties = Ice::createProperties(argc, argv);
        properties->setProperty("Ice.Default.EncodingVersion", "1.0");
        Ice::InitializationData initializationData;
        initializationData.properties = properties;
        state.iceCommunicator = Ice::initialize(initializationData);
    } catch (const Ice::Exception& e) {
        daemon_log(LOG_ERR, "error: unable to initialize ICE: %s", e.what());
        daemon_retval_send(EXIT_FAILURE);
        goto done;
    }
    try {
        auto connectionProxy = state.iceCommunicator->stringToProxy("Meta:tcp -h 192.168.1.200 -p 6502 -t 1000");
        state.connection = connectionProxy->ice_getConnection();
        auto localAddress = Ice::TCPConnectionInfoPtr::dynamicCast(state.connection->getInfo())->localAddress;
        state.FormClientEndpoint(localAddress);
        auto metaProxy = Murmur::MetaPrx::checkedCast(connectionProxy);
        for (auto serverProxy: metaProxy->getBootedServers()) {
            if (!state.server) {
                state.server = serverProxy;
            }
        }
        if (!state.server) {
            daemon_log(LOG_ERR, "No server!");
            daemon_retval_send(EXIT_FAILURE);
            goto done;
        }
        daemon_log(LOG_INFO, "Server: %d", (int)state.server->id());
    } catch (const Ice::Exception& e) {
        daemon_log(LOG_ERR, "error: %s", e.what());
        daemon_retval_send(EXIT_FAILURE);
        goto done;
    }
    daemon_log(LOG_INFO, "Successfully started");
    daemon_retval_send(EXIT_SUCCESS);

    try {
        auto servant = new ServerCallback(state);
        auto adapter = state.iceCommunicator->createObjectAdapterWithEndpoints("", state.clientEndpoint);
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
            daemon_log(LOG_INFO, "User: %s(%d)", user.second.name.c_str(), (int)user.second.userid);
            state.AddUser(user.second);
        }

        const int fd = daemon_signal_fd();
        while (!state.quit) {
            fd_set fds;
            FD_ZERO(&fds);
            FD_SET(fd, &fds);
            if (select(FD_SETSIZE, &fds, 0, 0, 0) < 0) {
                if (errno == EINTR) {
                    continue;
                }
                daemon_log(LOG_ERR, "select(): %s", strerror(errno));
                break;
            }
            if (FD_ISSET(fd, &fds)) {
                const int sig = daemon_signal_next();
                if (sig <= 0) {
                    daemon_log(LOG_ERR, "daemon_signal_next(): %s", strerror(errno));
                    break;
                }
                switch (sig) {
                    case SIGINT:
                    case SIGQUIT:
                    case SIGTERM: {
                        daemon_log(LOG_WARNING, "Got SIGINT, SIGQUIT, or SIGTERM");
                        state.quit = true;
                    } break;

                    case SIGHUP: {
                        daemon_log(LOG_INFO, "Got SIGHUP");
                        break;
                    } break;

                    default: break;
                }
            }
            std::ostringstream buf;
            buf << "Users: [";
            bool first = true;
            std::unique_lock< std::mutex > lock(state.mutex);
            for (auto user: state.users) {
                if (!first) {
                    buf << ' ';
                }
                first = false;
                buf << user.name << '(' << user.userid << ')';
            }
            buf << "]";
            daemon_log(LOG_INFO, "%s", buf.str().c_str());
        }
        daemon_log(LOG_INFO, "Done.");
    } catch (const Ice::Exception& e) {
        daemon_log(LOG_ERR, "error: %s", e.what());
    }
done:
    daemon_log(LOG_INFO, "Exiting...");
    daemon_signal_done();
    daemon_pid_file_remove();
    return EXIT_SUCCESS;
}
