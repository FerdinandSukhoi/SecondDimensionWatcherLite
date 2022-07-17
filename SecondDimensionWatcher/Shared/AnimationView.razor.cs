using System;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using BencodeNET.Objects;
using BencodeNET.Parsing;
using ByteSizeLib;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using SecondDimensionWatcher.Data;
using SecondDimensionWatcher.Services;

namespace SecondDimensionWatcher.Shared
{
    public partial class AnimationView : IDisposable
    {
        public enum TorrentStatus
        {
            UnTracked,
            Running,
            Paused,
            Finished,
            Error
        }


        [Inject] public FeedService FeedService { get; set; }

        [Inject] public IMemoryCache MemoryCache { get; set; }

        [Inject] public AppDataContext DbContext { get; set; }

        [Inject] public HttpClient Http { get; set; }

        [Inject] public ILogger<AnimationView> Logger { get; set; }

        [Inject] public IJSRuntime JSRuntime { get; set; }

        [Inject] public IConfiguration Configuration { get; set; }

        [Parameter] public Action<AnimationInfo> ModalCallBack { get; set; }

        [Parameter] public AnimationInfo AnimationInfo { get; set; }

        public TorrentInfo Info { get; set; }
        public string ProgressClass { get; set; } = string.Empty;
        public double ProgressValue { get; set; }

        public string RemainTimeString => new TimeSpan(0, 0, Info?.Eta ?? 0)
            .ToString("g", CultureInfo.CurrentCulture);

        public string SpeedString => ByteSize.FromBytes(Info?.Speed ?? 0).ToString("#.#") + "/s";

        public TorrentStatus Status { get; set; }
        [Inject] public QBitTorrentService QBitTorrent { get; set; }
        public Task FetchTask { get; set; }
        public CancellationTokenSource TokenSource { get; } = new();

        public void Dispose()
        {
            TokenSource?.Cancel();
        }

        public void SetSuitableClass()
        {
            ProgressClass = Status switch
            {
                TorrentStatus.UnTracked => "",
                TorrentStatus.Running => "progress-bar progress-bar-animated progress-bar-striped",
                TorrentStatus.Paused => "progress-bar progress-bar-striped bg-warning",
                TorrentStatus.Finished => "progress-bar progress-bar-striped bg-success",
                TorrentStatus.Error => "progress-bar progress-bar-striped bg-danger",
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        public TorrentStatus GetStatusFromString(string state)
        {
            if (state == "uploading" || state.Contains("UP"))
                return TorrentStatus.Finished;
            if (state is "pausedDL" or "checkingResumeData")
                return TorrentStatus.Paused;
            if (state == "downloading" || state.Contains("DL"))
                return TorrentStatus.Running;
            return TorrentStatus.Error;
        }

        public void BeginTrack()
        {
            FetchTask = InvokeAsync(async () =>
            {
                var token = TokenSource.Token;

                while (Status != TorrentStatus.Finished || !token.IsCancellationRequested)
                {
                    var info = MemoryCache.Get<TorrentInfo>(AnimationInfo.Hash);
                    if (info == null)
                    {
                        await Task.Delay(150, token);
                        info = MemoryCache.Get<TorrentInfo>(AnimationInfo.Hash.ToUpper());
                        if (info == null)
                        {
                            AnimationInfo = await DbContext.AnimationInfo.FindAsync(AnimationInfo.Id);
                            Info = new()
                            {
                                Eta = 0,
                                Speed = 0,
                                Hash = AnimationInfo.Hash,
                                Progress = 1,
                                SavePath = AnimationInfo.StorePath
                            };
                            Status = TorrentStatus.Finished;
                            ProgressValue = 1;
                            SetSuitableClass();
                            StateHasChanged();
                            return;
                        }
                    }

                    Info = info;
                    Status = GetStatusFromString(Info.State);
                    SetSuitableClass();
                    ProgressValue = Info.Progress;
                    StateHasChanged();
                    await Task.Delay(1000, token);
                }
            });
        }

        protected override void OnParametersSet()
        {
            if (AnimationInfo.IsTracked)
                BeginTrack();
        }

        public async ValueTask Pause()
        {
            await QBitTorrent.Pause(AnimationInfo.Hash);
        }

        public async ValueTask Resume()
        {
            await QBitTorrent.Resume(AnimationInfo.Hash);
        }

        public async Task OpenDetailPage()
        {
            await JSRuntime.InvokeVoidAsync("newPage",
                $"/file/{AnimationInfo.Hash}");
        }

        public void Delete()
        {
            ModalCallBack(AnimationInfo);
        }

        public async ValueTask Download()
        {
            var animationInfo = await DbContext.AnimationInfo.AsTracking().FirstOrDefaultAsync(
                a=>a.Id== AnimationInfo.Id);
            if (animationInfo is null|| animationInfo.TorrentData is not null)
                return;
            animationInfo.TorrentData = await Http.GetByteArrayAsync(animationInfo.TorrentUrl);
            var parser = new BencodeParser();
            animationInfo.Hash = BitConverter
                .ToString(SHA1.HashData(
                    parser.Parse<BDictionary>(animationInfo.TorrentData)["info"]
                        .EncodeAsBytes()))
                .Replace("-", "");
            await DbContext.SaveChangesAsync();
            
            if (await QBitTorrent.Add(animationInfo.TorrentData))
            {
                animationInfo.IsTracked = true;
                animationInfo.TrackTime = DateTimeOffset.Now.ToUnixTimeSeconds();
                await DbContext.SaveChangesAsync();
                AnimationInfo.IsTracked = true;
                AnimationInfo.Hash = animationInfo.Hash;
                BeginTrack();
                Logger.LogInformation($"The torrent {animationInfo.Description} successfully added.");
            }
        }
    }
}