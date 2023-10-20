using Il2CppSystem.Dynamic.Utils;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using Nebula.Configuration;
using OpusDotNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nebula.VoiceChat;

public class VCClient : IDisposable
{
    private OpusDotNet.OpusDecoder myDecoder;
    private BufferedWaveProvider bufferedProvider;
    private VolumeSampleProvider volumeFilter;
    private PanningSampleProvider panningFilter;
    private PlayerControl relatedControl;
    private PlayerModInfo? relatedInfo = null;
    public MixingSampleProvider? myRoute = null;
    private float wallRatio = 1f;
    private bool onRadio = false;
    private int radioMask;
    public VoiceType VoiceType { get; private set; }

    public void SetVoiceType(VoiceType voiceType)
    {
        if (voiceType == VoiceType) return;

        VoiceType = voiceType;
        var route = NebulaGameManager.Instance!.VoiceChatManager?.GetRoute(voiceType);
        SetRoute(route);
    }

    public bool IsValid => relatedControl;
    public bool CanHear => !relatedControl.AmOwner && (PlayerControl.LocalPlayer.Data.IsDead || !relatedControl.Data.IsDead);
    public VCClient(PlayerControl player) {
        relatedControl = player;

        myDecoder = new(24000, 1);
        bufferedProvider = new(new(22050, 1));
        var floatConverter = new WaveToSampleProvider(new Wave16ToFloatProvider(bufferedProvider));
        volumeFilter = new(floatConverter);
        panningFilter = new(volumeFilter);
        panningFilter.Pan = 0f;
    }

    public void OnGameStart()
    {
        relatedInfo = NebulaGameManager.Instance!.GetModPlayerInfo(relatedControl.PlayerId);
    }

    private void UpdateAudio()
    {
        bool aliveAll = AmongUsClient.Instance.GameState != InnerNet.InnerNetClient.GameStates.Started;

        if (!aliveAll && !CanHear) return;

        //互いに死んでいる場合
        if (!aliveAll && PlayerControl.LocalPlayer.Data.IsDead && relatedControl.Data.IsDead)
        {
            volumeFilter.Volume = 1f;
            panningFilter.Pan = 0f;
            return;
        }

        //ラジオ
        if ((aliveAll || !relatedControl.Data.IsDead) && VoiceType == VoiceType.Radio)
        {
            volumeFilter.Volume = ((1 << PlayerControl.LocalPlayer.PlayerId) & radioMask) == 0 ? 0f : 1f;
            panningFilter.Pan = 0f;
            return;
        }

        //会議中
        if (VoiceChatManager.IsInDiscussion)
        {
            volumeFilter.Volume = (PlayerControl.LocalPlayer.Data.IsDead || !relatedControl.Data.IsDead) ? 1f : 0f;
            panningFilter.Pan = 0f;
            return;
        }

        //コミュサボ
        if (AmongUsClient.Instance.GameState == InnerNet.InnerNetClient.GameStates.Started && GeneralConfigurations.AffectedByCommsSabOption && !PlayerControl.LocalPlayer.Data.IsDead && (!PlayerControl.LocalPlayer.Data.Role?.IsImpostor ?? true) && AmongUsUtil.InCommSab)
        {
            volumeFilter.Volume = 0f;
            return;
        }

        try
        {
            var lightRadius = PlayerControl.LocalPlayer.lightSource.viewDistance;
            Vector2 ownerPos = PlayerControl.LocalPlayer.transform.position;
            Vector2 myPos = relatedControl.transform.position;

            float distance = myPos.Distance(ownerPos);

            if (GeneralConfigurations.WallsBlockAudioOption && PhysicsHelpers.AnyNonTriggersBetween(ownerPos, (myPos - ownerPos).normalized, distance, Constants.ShadowMask))
                wallRatio *= 0.9f;
            else
                wallRatio += (1f - wallRatio) * 0.9f;

            float distanceRatio = 1f;

            if (distance > lightRadius * 1.7f)
                distanceRatio = 0f;
            else if (distance > lightRadius * 0.7f)
                distanceRatio = 1f - (distance - lightRadius * 0.7f) / (lightRadius * 1f);

            volumeFilter.Volume = Mathf.Clamp01(distanceRatio) * wallRatio;

            float xDis = myPos.x - ownerPos.x;

            panningFilter.Pan = Mathf.Clamp(xDis / 1.4f, -1f, 1f);
        }
        catch
        {
            volumeFilter.Volume = 0f;
            panningFilter.Pan = 0f;
        }
    }

    public void Update()
    {
        UpdateAudio();
    }

    public void Dispose()
    {
        myDecoder?.Dispose();
        myDecoder = null!;

        SetRoute(null);
    }

    public ISampleProvider MyProvider { get => panningFilter; }

    private byte[] rawAudioData = new byte[5760];
    public void OnReceivedData(bool isRadio, int radioMask, byte[] data)
    {
        onRadio = isRadio;
        SetVoiceType((isRadio && !(relatedControl.Data?.IsDead ?? false)) ? VoiceType.Radio : VoiceType.Normal);
    

        if(VoiceType != VoiceType.Radio)
        {
            if ((relatedControl.Data?.IsDead ?? false) && VoiceChatManager.CanListenGhostVoice) 
                SetVoiceType(VoiceType.Ghost);
            else
                SetVoiceType(VoiceType.Normal);
        }

        //聴こえない音に対しては何もしない
        if (VoiceType != VoiceType.Ghost && !CanHear) return;

        this.radioMask = radioMask; 

        int rawSize = myDecoder!.Decode(data, data.Length, rawAudioData, rawAudioData.Length);

        try
        {
            if (bufferedProvider!.BufferedBytes == 0)
                bufferedProvider!.AddSamples(new byte[1024], 0, 1024);

            bufferedProvider!.AddSamples(rawAudioData, 0, rawSize);
        }
        catch (Exception e){
            Debug.Log(e.Message);
        }
    }

    public void SetRoute(MixingSampleProvider? route)
    {
        if (myRoute != null) myRoute.RemoveMixerInput(MyProvider);
        myRoute = route;
        myRoute?.AddMixerInput(MyProvider);
    }
}
