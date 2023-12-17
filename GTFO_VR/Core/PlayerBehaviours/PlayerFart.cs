using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using GTFO_VR.Events;
using Il2CppInterop.Runtime;
using Player;
using SteamVR_Standalone_IL2CPP.Util;
using UnityEngine;
using Valve.VR;
using Application = UnityEngine.Application;
using Object = UnityEngine.Object;

namespace GTFO_VR.Core.PlayerBehaviours
{
    public class PlayerFart : MonoBehaviour
    {
        private float initialMinTimeForNextFart = 5; // in seconds
        private float fartDelay = 1; // in seconds

        public List<AudioClip> clips; 
        private float fartTimer = 0;
        private bool initialDelay = true;
        public int fartCount = 0;
        public bool canFart = false;
        private PlayerLocomotion.PLOC_State m_lastLocState;
        private PlayerChatManager m_chatManager;
        private GameObject audioListener;

        private SteamVR_Action_Boolean m_crouch;

        public PlayerFart(IntPtr value) : base(value)
        {
        }

        private void Awake()
        {
            PlayerLocomotionEvents.OnStateChange += OnPlayerJumpFarted;
            ChatMsgEvents.OnChatMsgReceived += ChatMsgReceived;
        }
        private void FixedUpdate()
        {
            if (audioListener == null)
            {
                audioListener = GameObject.Find("GLOBAL/Managers/Chat/DissonanceSetup");
            }
            if (audioListener != null)
            {
                audioListener.transform.position = VRPlayer.FpsCamera.transform.position;
            }

            // fart timer
            float delay = initialDelay ? initialMinTimeForNextFart : fartDelay;
            if (fartTimer >= delay)
            {
                canFart = true;
                initialDelay = false;
                fartTimer = 0;
            }
            else
            {
                fartTimer += Time.deltaTime;
            }

            //chat manager
            if (!m_chatManager)
            {
                m_chatManager = PlayerChatManager.Current;
            }
        }

        public void Setup()
        {
            clips = new List<AudioClip>();
            GetClipsFromFolder();
            m_crouch = SteamVR_Input.GetBooleanAction("Crouch", false);
            m_crouch.AddOnStateDownListener(OnCrouchInput, SteamVR_Input_Sources.Any);
            
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
                SendChatMessage();
            }

            m_lastLocState = state;
        }

        private void OnCrouchInput(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
        {
            SendChatMessage();
        }

        public void PlayFart(Vector3 position)
        {
            if(canFart)
            {
                if (fartCount > (clips.Count - 1))
                {
                    fartCount = 0;
                }
                AudioClip clip = clips[fartCount];
                AudioSource.PlayClipAtPoint(clip, position, 1f);
                fartCount++;
                canFart = false;
            }
        }

        public void SendChatMessage()
        {
            if (canFart && m_chatManager && VRPlayer.FpsCamera)
            {
                string pos = VRPlayer.FpsCamera.transform.position.ToString();
                string code = String.Join("_", String.Join("", pos.Split('(', ' ', ')')).Split(','));
                string msg = "error_frt_" + code;
                m_chatManager.m_currentValue = msg;
                m_chatManager.PostMessage();
            }
        }

        public void ChatMsgReceived(string msg)
        {
            if (canFart)
            {
                Vector3 pos = new Vector3();
                var parts = msg.Split("error_frt_")[1].Split("_");

                pos.x = Convert.ToSingle(parts[0], CultureInfo.InvariantCulture);
                pos.y = Convert.ToSingle(parts[1], CultureInfo.InvariantCulture);
                pos.z = Convert.ToSingle(parts[2], CultureInfo.InvariantCulture);
                PlayFart(pos);
            }
        }
    }
}