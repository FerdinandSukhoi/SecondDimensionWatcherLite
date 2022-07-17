using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using BencodeNET.Objects;
using BencodeNET.Parsing;
using Microsoft.Extensions.Configuration;
using SecondDimensionWatcher.Data;

namespace SecondDimensionWatcher.Services;

public class QBitTorrentService
{
    private readonly IConfiguration _configuration;
    private readonly HttpClient _http;
    private string ApiUrl{ get; }
    public QBitTorrentService(IConfiguration configuration,HttpClient http)
    {
        _configuration = configuration;
        ApiUrl = $"{_configuration["DownloadSetting:BaseAddress"]}api/v2/torrents/";
        _http = http;
    }

    public async ValueTask<TorrentInfo[]> GetTorrentStatus(string hash, CancellationToken cancellationToken)
    {
        var response = await _http.GetStreamAsync($"{ApiUrl}info?hashes=" + hash, cancellationToken);
        return await JsonSerializer.DeserializeAsync<TorrentInfo[]>(response, cancellationToken: cancellationToken);
    }
    public async Task<bool> Delete(params string[] hashes)
    {
        if (hashes.Any())
        {
            var response = await _http.GetAsync($"{ApiUrl}delete?hashes={string.Join('|', hashes)}&deleteFiles=true");
            return response.IsSuccessStatusCode;
        }

        return true;
    }
    public async Task<bool> Pause(params string[] hashes)
    {
        if (hashes.Any())
        {
            var response = await _http.GetAsync($"{ApiUrl}pause?hashes={string.Join('|', hashes)}");
            return response.IsSuccessStatusCode;
        }

        return true;
    }
    public async Task<bool> Resume(params string[] hashes)
    {
        if (hashes.Any())
        {
            var response = await _http.GetAsync($"{ApiUrl}resume?hashes={string.Join('|', hashes)}");
            return response.IsSuccessStatusCode;
        }

        return true;
    }

    public async Task<bool> Add(byte[] torrentFile, params string[] tags)
    {
        //var parser = new BencodeParser();
        //var seedHash = BitConverter
        //    .ToString(SHA1.HashData(
        //        parser.Parse<BDictionary>(torrentFile)["info"]
        //            .EncodeAsBytes()))
        //    .Replace("-", "");
        var content = new MultipartFormDataContent
            { { new ByteArrayContent(torrentFile), "torrent", $"animation.torrent" } };
        content.Add(new StringContent(Path.GetFullPath(_configuration["DownloadDir"])), "savepath");
        content.Add(new StringContent("animation"), "category");
        if (tags.Length > 0)
            content.Add(new StringContent(string.Join(',', tags)), "tags");
        var response = await _http.PostAsync($"{ApiUrl}add", content);
        return response.IsSuccessStatusCode;
    }

}