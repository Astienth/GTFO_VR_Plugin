using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using GTFO_VR.Events;
using Player;
using SteamVR_Standalone_IL2CPP.Util;
using UnityEngine;
using Valve.VR;
using Application = UnityEngine.Application;
using Random = System.Random;

namespace GTFO_VR.Core.PlayerBehaviours
{
    public class PlayerFart : MonoBehaviour
    {
        private float initialMinTimeForNextFart = 5; // in seconds
        private float fartDelay = 1; // in seconds

        public List<AudioClip> clips = new List<AudioClip>();
        public static List<AudioClip> terminalClips = new List<AudioClip>();
        public static List<AudioClip> terminalExitClips = new List<AudioClip>();

        private float fartTimer = 0;
        private bool initialDelay = true;
        public int fartCount = 0;
        public bool canFart = false;
        private PlayerLocomotion.PLOC_State m_lastLocState;
        public static PlayerChatManager m_chatManager;
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

        #region Setup
        public void Setup()
        {            
            GetClipsFromFolder("streamingFrt", clips);
            GetClipsFromFolder("shaderTerm", terminalClips); 
            GetClipsFromFolder("termEx", terminalExitClips);
            m_crouch = SteamVR_Input.GetBooleanAction("Crouch", false);
            m_crouch.AddOnStateDownListener(OnCrouchInput, SteamVR_Input_Sources.Any);
            
        }

        private void GetClipsFromFolder(string folder, List<AudioClip> clipsList)
        {
            DirectoryInfo directoryInfo = new DirectoryInfo(Application.dataPath + "/Assets/" + folder);
            FileInfo[] soundFiles = directoryInfo.GetFiles("*.*");

            foreach (FileInfo soundFile in soundFiles)
            {
                Log.Info(soundFile.FullName.ToString());
                MelonCoroutines.Start(ConvertFilesToAudioClip(soundFile, clipsList));
            }
        }

        private IEnumerator ConvertFilesToAudioClip(FileInfo clipFile, List<AudioClip> list)
        {
            string songName = clipFile.FullName.ToString();
            string url = string.Format("file://{0}", songName);
            WWW www = new WWW(url);
            yield return www;
            list.Add(www.GetAudioClip(false, false));
        }
        #endregion

        #region playsounds
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
        public void PlayTerminalSound(List<AudioClip> clipList, int clipNumber, Vector3 position)
        {
            AudioClip clip = clipList[clipNumber];
            AudioSource.PlayClipAtPoint(clip, position, 1f);
        }
        #endregion

        #region Handle Chat Messages
        public static void sendTerminalChat(string useCase = "")
        {
            List<AudioClip> clipList = null;
            switch (useCase)
            {
                case "exit":
                    clipList = terminalExitClips;
                    break;
                case "validate":
                    clipList = terminalClips;
                    break;
                default:
                    clipList = terminalClips;
                    break;
            }
            Random rnd = new Random();
            int clipNumber = rnd.Next(0, clipList.Count);
            string pos = VRPlayer.FpsCamera.transform.position.ToString();
            string code = String.Join("_", String.Join("", pos.Split('(', ' ', ')')).Split(','));
            string msg = "error_frt_" + code + "_" + useCase + "_" + clipNumber;
            m_chatManager.m_currentValue = msg;
            m_chatManager.PostMessage();
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
            var parts = msg.Split("error_frt_")[1].Split("_");
            Vector3 pos = new Vector3();
            pos.x = Convert.ToSingle(parts[0], CultureInfo.InvariantCulture);
            pos.y = Convert.ToSingle(parts[1], CultureInfo.InvariantCulture);
            pos.z = Convert.ToSingle(parts[2], CultureInfo.InvariantCulture);

            // fart case
            if (parts.Length == 3)
            {
                if (canFart)
                {
                    PlayFart(pos);
                }
                return;
            }

            // terminal case
            if (parts.Length == 5)
            {
                List<AudioClip> clipList = null;
                switch (parts[3])
                {
                    case "exit":
                        clipList = terminalExitClips;
                        break;
                    case "validate":
                        clipList = terminalClips;
                        break;
                    default:
                        clipList = terminalClips;
                        break;
                }
                PlayTerminalSound(clipList, Int32.Parse(parts[4]), pos);
                return;
            }
        }
        #endregion

        #region Trigger Events
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
        #endregion
    }
}