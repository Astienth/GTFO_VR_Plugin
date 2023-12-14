using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using Bhaptics.SDK2;
using GTFO_VR.Core.VR_Input;
using GTFO_VR.Events;
using Player;
using SteamVR_Standalone_IL2CPP.Util;
using UnityEngine;
using UnityEngine.Networking;
using static System.Net.Mime.MediaTypeNames;
using Application = UnityEngine.Application;

namespace GTFO_VR.Core.PlayerBehaviours
{
    public class PlayerFart : MonoBehaviour
    {
        public AudioSource audioSource;
        private bool m_lastIsCrouchedPhysically; 
        public List<AudioClip> clips;
        public int crouchCount = 0;
        public int fartCount = 0;
        private PlayerLocomotion.PLOC_State m_lastLocState;

        public PlayerFart(IntPtr value) : base(value)
        {
        }

        private void Awake()
        {
            PlayerLocomotionEvents.OnStateChange += OnPlayerJumpFarted;
        }

        public void Setup()
        {
            clips = new List<AudioClip>();
            audioSource = gameObject.AddComponent<AudioSource>();
            GetClipsFromFolder();
        }

        private void GetClipsFromFolder()
        {
            DirectoryInfo directoryInfo = new DirectoryInfo(Application.dataPath + "/Assets");
            FileInfo[] soundFiles = directoryInfo.GetFiles("*.*");

            foreach (FileInfo soundFile in soundFiles)
            {
                Log.Info(soundFile.FullName.ToString());
                MelonCoroutines.Start(ConvertFilesToAudioClip(soundFile));
            }
        }

        private IEnumerator ConvertFilesToAudioClip(FileInfo clipFile)
        {
            string songName = clipFile.FullName.ToString();
            string url = string.Format("file://{0}", songName);
            WWW www = new WWW(url);
            yield return www;
            clips.Add(www.GetAudioClip(false, false));
        }

        public void OnPlayerJumpFarted(PlayerLocomotion.PLOC_State state)
        {

            if ((m_lastLocState == PlayerLocomotion.PLOC_State.Fall || m_lastLocState == PlayerLocomotion.PLOC_State.Jump)
                && (state == PlayerLocomotion.PLOC_State.Stand || state == PlayerLocomotion.PLOC_State.Crouch))
            {
                PlayFart();
            }

            m_lastLocState = state;
        }
        /*
        private void FixedUpdate()
        {
            bool isCrouchedPhysically = IsCrouchedPhysically();
            if (m_lastIsCrouchedPhysically != isCrouchedPhysically)
            {
                if (isCrouchedPhysically)
                {
                    PlayFart();
                }
                m_lastIsCrouchedPhysically = isCrouchedPhysically;
            }
        }
        */

        private bool IsCrouchedPhysically()
        {
            return HMD.Hmd.transform.localPosition.y + VRConfig.configFloorOffset.Value / 100f < VRConfig.configCrouchHeight.Value / 100f;
        }

        public void PlayFart()
        {
            if (fartCount > (clips.Count -1))
            {
                fartCount = 0;
            }
            AudioClip clip = clips[fartCount];
            audioSource.PlayOneShot(clip);
            fartCount++;
        }
    }
}