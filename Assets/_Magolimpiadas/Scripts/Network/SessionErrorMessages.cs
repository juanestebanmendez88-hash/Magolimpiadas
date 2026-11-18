using Fusion;

namespace ArcaneGames.Network
{
    public static class SessionErrorMessages
    {
        public const string Connecting = "Conectando con el servidor...";
        public const string InvalidName = "Escribe un nombre de 1 a 16 letras o números.";
        public const string InvalidRoomCode = "El código de sala tiene 4 letras.";
        public const string AlreadyConnecting = "Ya se está conectando, espera un momento.";
        public const string SettingsMissing = "Falta la configuración del modo en línea.";
        public const string SessionClosed = "Se cerró la sala.";
        public const string RoomSetupFailed = "No se pudo preparar la sala. Intenta crearla otra vez.";

        public static string ForStartFailure(ShutdownReason reason)
        {
            switch (reason)
            {
                case ShutdownReason.GameNotFound:
                    return "No existe una sala con ese código.";
                case ShutdownReason.GameIsFull:
                    return "Esa sala ya tiene 4 jugadores.";
                case ShutdownReason.GameClosed:
                    return "La partida de esa sala ya empezó.";
                case ShutdownReason.PhotonCloudTimeout:
                case ShutdownReason.ConnectionTimeout:
                case ShutdownReason.OperationTimeout:
                    return "No hay conexión con el servidor. Revisa tu internet.";
                default:
                    return "No se pudo conectar (" + reason + ").";
            }
        }

        public static string ForDisconnect(ShutdownReason reason)
        {
            switch (reason)
            {
                case ShutdownReason.DisconnectedByPluginLogic:
                case ShutdownReason.ConnectionRefused:
                case ShutdownReason.Ok:
                    return "El anfitrión cerró la sala.";
                case ShutdownReason.PhotonCloudTimeout:
                case ShutdownReason.ConnectionTimeout:
                case ShutdownReason.OperationTimeout:
                    return "Se perdió la conexión con la sala.";
                default:
                    return SessionClosed + " (" + reason + ")";
            }
        }
    }
}
