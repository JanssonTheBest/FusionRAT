using NLog;

namespace Server.UI.CustomControls.NLog
{
    public class CustomLogger
    {
        private readonly Logger _logger;

        public CustomLogger(Logger logger)
        {
            _logger = logger;
        }

        public void Success(string message)
        {
            _logger.Debug(message);
        }

        public void Info(string message)
        {
            _logger.Info(message);
        }

        public void Warn(string message)
        {
            _logger.Warn(message);
        }

        public void Error(string message)
        {
            _logger.Error(message);
        }

        public void Fatal(string message)
        {
            _logger.Fatal(message);
        }
    }
}