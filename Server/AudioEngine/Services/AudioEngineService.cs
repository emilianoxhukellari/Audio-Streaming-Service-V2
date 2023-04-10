using DataAccess.Contexts;
using DataAccess.Services;
using Microsoft.EntityFrameworkCore;
using Server_Application.Server;
using System;
using System.Collections.Generic;
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

        public event EventHandler<bool>? ServerStateChanged;
        public bool IsRunning { get; set; }

        public AudioEngineService(IDbContextFactory<StreamingDbContext> dbContextFactory, IServiceProvider serviceProvider, IAudioEngineConfigurationService audioEngineConfigurationService)
        {
            _dbContextFactory = dbContextFactory;
            _serviceProvider = serviceProvider;
            _audioEngineConfigurationService = audioEngineConfigurationService;
            _controller = new Controller(_dbContextFactory, _serviceProvider, _audioEngineConfigurationService);
        }

        public void SetDesktopSearchLimit(int limit)
        {
            if(limit < 1) limit = 1; // At least one song to be displayed

            _audioEngineConfigurationService.DesktopSongSearchLimit = limit; 

        }

        public async Task StartEngineAsync()
        {
            await Task.Run(() =>
            {
                _controller.Start();
            });

            if (IsRunning != _controller.IsRunning)
            {
                IsRunning = _controller.IsRunning;
                ServerStateChanged?.Invoke(this, IsRunning);
            }
        }

        public async Task StopEngineAsync()
        {
            await Task.Run(() =>
            {
                _controller.Stop();
            });


            if (IsRunning != _controller.IsRunning)
            {
                IsRunning = _controller.IsRunning;
                ServerStateChanged?.Invoke(this, IsRunning);
            }
        }
    }
}
