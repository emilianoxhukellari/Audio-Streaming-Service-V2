using DataAccess.Contexts;
using DataAccess.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Server_Application.Server;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AudioEngine.Services
{
    public class AudioEngineService : IAudioEngineService
    {
        private readonly IDbContextFactory<StreamingDbContext> _dbContextFactory;
        private readonly IServiceProvider _serviceProvider;
        private readonly IAudioEngineConfigurationService _audioEngineConfigurationService;
        private readonly Controller _controller;
        private readonly Task _loadUpdateTask;

        public event EventHandler<bool>? ServerStartStopChanged;
        public event EventHandler<int>? ServerDesktopSongLimitChanged;
        public event EventHandler<int>? ServerWebSongLimitChanged;
        public event EventHandler<int>? ServerDesktopClientCountLimitChanged;
        public event EventHandler<ServerLoadArgs>? ServerLoadUpdate;

        public class ServerLoadArgs : EventArgs
        {
            public int ConnectedClientsCount { get; set; }
            public int ClientCountLimit { get; set; }
            public int Percentage
            {
                get
                {
                    return ClientCountLimit == 0 ? 0 : (int)((double)ConnectedClientsCount / ClientCountLimit * 100);
                }
            }
        }

        public bool IsRunning { get; set; }


        public AudioEngineService(IDbContextFactory<StreamingDbContext> dbContextFactory, IServiceProvider serviceProvider, IAudioEngineConfigurationService audioEngineConfigurationService)
        {
            _dbContextFactory = dbContextFactory;
            _serviceProvider = serviceProvider;
            _audioEngineConfigurationService = audioEngineConfigurationService;
            _controller = new Controller(_dbContextFactory, _serviceProvider, _audioEngineConfigurationService);
            _loadUpdateTask = new Task(LoadUpdateLoop);
            _loadUpdateTask.Start();
        }

        /// <summary>
        /// This method will fire ServerLoadUpdate when ConnectedCount or DesktopClientCountLimit changes.
        /// It checks for updates every 500 ms.
        /// </summary>
        private void LoadUpdateLoop()
        {
            int connectedCount = 0;
            int clientCountLimit = 0;

            while (true)
            {
                if (connectedCount != _controller.ConnectedCount || clientCountLimit != GetDesktopClientCountLimit())
                {
                    connectedCount = _controller.ConnectedCount;
                    clientCountLimit = GetDesktopClientCountLimit();

                    ServerLoadUpdate?.Invoke(this,
                        new ServerLoadArgs
                        {
                            ClientCountLimit = clientCountLimit,
                            ConnectedClientsCount = connectedCount,
                        });
                }
                Thread.Sleep(500);
            }
        }

        // Get Init State

        public ServerLoadArgs GetServerLoadInitialState()
        {
            return new ServerLoadArgs
            {
                ClientCountLimit = GetDesktopClientCountLimit(),
                ConnectedClientsCount = _controller.ConnectedCount,
            };
        }

        public void SetDesktopAppSongSearchLimit(int limit)
        {
            if (limit < 0) limit = 0;

            using (var scope = _serviceProvider.CreateScope())
            {
                var dataAccessConfigurationService = scope.ServiceProvider.GetRequiredService<IDataAccessConfigurationService>();
                dataAccessConfigurationService.DesktopAppSongSearchLimit = limit;
                ServerDesktopSongLimitChanged?.Invoke(this, limit);
            }
        }

        public int GetDesktopAppSongSearchLimit()
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var dataAccessConfigurationService = scope.ServiceProvider.GetRequiredService<IDataAccessConfigurationService>();
                return dataAccessConfigurationService.DesktopAppSongSearchLimit;
            }
        }

        public void SetWebAppSongSearchLimit(int limit)
        {
            if (limit < 0) limit = 0;

            using (var scope = _serviceProvider.CreateScope())
            {
                var dataAccessConfigurationService = scope.ServiceProvider.GetRequiredService<IDataAccessConfigurationService>();
                dataAccessConfigurationService.WebAppSongSearchLimit = limit;
                ServerWebSongLimitChanged?.Invoke(this, limit);
            }
        }

        public int GetWebAppSongSearchLimit()
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var dataAccessConfigurationService = scope.ServiceProvider.GetRequiredService<IDataAccessConfigurationService>();
                return dataAccessConfigurationService.WebAppSongSearchLimit;
            }
        }

        public void SetDesktopClientCountLimit(int limit)
        {
            if (limit < 0) limit = 0;

            using (var scope = _serviceProvider.CreateScope())
            {
                var dataAccessConfigurationService = scope.ServiceProvider.GetRequiredService<IDataAccessConfigurationService>();
                dataAccessConfigurationService.DesktopAppClientCountLimit = limit;
                _controller.ChangeClientCountLimit(limit);
                ServerDesktopClientCountLimitChanged?.Invoke(this, limit);
            }
        }

        public int GetDesktopClientCountLimit()
        {
            return _controller.ClientCountLimit;
        }

        public async Task StartEngineAsync()
        {
            await Task.Run(_controller.Start);

            if (IsRunning != _controller.IsRunning)
            {
                IsRunning = _controller.IsRunning;
                ServerStartStopChanged?.Invoke(this, IsRunning);
            }
        }

        public async Task StopEngineAsync()
        {
            await Task.Run(_controller.Stop);

            if (IsRunning != _controller.IsRunning)
            {
                IsRunning = _controller.IsRunning;
                ServerStartStopChanged?.Invoke(this, IsRunning);
            }
        }
    }
}
