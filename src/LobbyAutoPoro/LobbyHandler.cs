using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Ekko;
using Newtonsoft.Json;

namespace LobbyAutoPoro;

public enum Region
{
    UNKNOWN,
    EUW,
    EUNE,
    NA,
    TR,
    OCE,
    LAN,
    LAS,
    RU,
    BR,
    JP
}

public enum Platform
{
    UNKNOWN,
    EUW1,
    EUN1,
    NA1,
    TR1,
    OC1,
    LA1,
    LA2,
    RU,
    BR1,
    JP1
}
public delegate void OnUpdate(LobbyHandler handler);
public class LobbyHandler
{
    private readonly LeagueApi _api;
    private string[] _cache;
    private Region? _region;

    private string? user_raw;
    private string? participants_raw;

    private string? session_raw;

    public LobbyHandler(LeagueApi api)
    {
        _api = api;
        _cache = Array.Empty<string>();
    }
    public OnUpdate OnUpdate;

    public void Start()
    {
        new Thread(async () => { await Loop(); })
        {
            IsBackground = true
        }.Start();
    }

    public string[] GetSummoners()
    {
        return _cache;
    }

    public Region? GetRegion()
    {
        return _region;
    }

    public string GetParticipantsJson()
    {
        return participants_raw ?? "";
    }

    public string GetUserJson()
    {
        return user_raw ?? "";
    }

    public string GetSessionJson()
    {
        return session_raw ?? "";
    }
    private async Task Loop()
    {
        while (true)
        {
            Thread.Sleep(2000);
            if (_region is null)
            {
                user_raw = await _api.SendAsync(HttpMethod.Get, "/rso-auth/v1/authorization/userinfo");
                if (string.IsNullOrWhiteSpace(user_raw))
                    continue;

                var resp1 = JsonConvert.DeserializeObject<Userinfo>(user_raw);
                if (resp1 is null)
                {
                    continue;
                }

                var resp2 = JsonConvert.DeserializeObject<UserinfoActual>(resp1.userinfo);
                if (resp2 is null)
                {
                    continue;
                }

                var reg = Enum.TryParse(resp2.lol.cpid, out Platform region);
                if (!reg)
                {
                    Console.WriteLine("Could not figure out region. Setting EUW");
                    _region = Region.EUW;
                }
                else
                {
                    _region = (Region)region;
                }
            }

            participants_raw = await _api.SendAsync(HttpMethod.Get, "/chat/v5/participants/champ-select");
            if (string.IsNullOrWhiteSpace(participants_raw))
                continue;

            session_raw = await _api.SendAsync(HttpMethod.Get, "/lol-champ-select/v1/session");
            

            var participantsJson = JsonConvert.DeserializeObject<Participants>(participants_raw);
            if (participantsJson is null)
                continue;

            var names = participantsJson.participants.Select(x => x.name).ToArray();

            if (!_cache.SequenceEqual(names) && names.Length > 4) //wait for full lobby to be loaded
            {
                _cache = names;
                OnUpdate?.Invoke(this);
            }
        }
    }
}