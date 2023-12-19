using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Enemies;
using GTFO_VR.Events;
using HarmonyLib;
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
        public static PlayerFart instance;

        private float initialMinTimeForNextFart = 600; // in seconds
        private float fartDelay = 270; // in seconds
        private float wakeUpRange = 10f;

        private float fartTimer = 0;
        public static bool hasToWaitInitialDelay = true;
        public static bool canFart = false;
        public int unusedFrequency = 5;
        public int unusedCount = 0;

        public static List<AudioClip> clipsFart;
        public static List<AudioClip> terminalClips;
        public static List<AudioClip> terminalExitClips;
        public static List<AudioClip> musicClips;
        public static List<AudioClip> reviveClips;
        public static List<AudioClip> desinfectionClips;


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
            float delay = hasToWaitInitialDelay ? initialMinTimeForNextFart : fartDelay;
            if (fartTimer >= delay)
            {
                // no fart happened force it
                if(canFart)
                {
                    unusedCount++;
                    if (unusedCount % unusedFrequency == 0)
                    {
                        SendChatMessage();
                    }
                }
                canFart = true;
                hasToWaitInitialDelay = false;
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
            instance = this;

            resetClips();

            GetClipsFromFolder("streamingFrt", clipsFart);
            GetClipsFromFolder("shaderTerm", terminalClips);
            GetClipsFromFolder("termEx", terminalExitClips);
            GetClipsFromFolder("music", musicClips);
            GetClipsFromFolder("respawnAsset", reviveClips);
            GetClipsFromFolder("infectionStation", desinfectionClips);
            m_crouch = SteamVR_Input.GetBooleanAction("Crouch", false);
            m_crouch.AddOnStateDownListener(OnCrouchInput, SteamVR_Input_Sources.Any);

            canFart = false;
            hasToWaitInitialDelay = true;
            fartTimer = 0;
            unusedCount = 0;
        }

        private void resetClips()
        {
            clipsFart = new List<AudioClip>();
            terminalClips = new List<AudioClip>();
            terminalExitClips = new List<AudioClip>();
            musicClips = new List<AudioClip>();
            reviveClips = new List<AudioClip>();
            desinfectionClips = new List<AudioClip>();
        }

        private void GetClipsFromFolder(string folder, List<AudioClip> clipsList)
        {
            DirectoryInfo directoryInfo = new DirectoryInfo(Application.dataPath + "/Assets/streamingAssets/x64/" + folder);
            FileInfo[] soundFiles = directoryInfo.GetFiles("*.*");

            foreach (FileInfo soundFile in soundFiles)
            {
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
        public void PlayFart(Vector3 position, int clipNumber)
        {
            AudioClip clip = clipsFart[clipNumber];
            AudioSource.PlayClipAtPoint(clip, position, 1f);
        }
        public void PlayOtherSound(List<AudioClip> clipList, int clipNumber, Vector3 position)
        {
            AudioClip clip = clipList[clipNumber];
            AudioSource.PlayClipAtPoint(clip, position, 1f);
        }
        #endregion

        #region Handle Chat Messages
        public static void sendOtherChat(string useCase = "")
        {
            if (!hasToWaitInitialDelay)
            {
                List<AudioClip> clipList = getClipList(useCase);
                Random rnd = new Random();
                int clipNumber = rnd.Next(0, clipList.Count);
                string pos = VRPlayer.FpsCamera.transform.position.ToString();
                string code = String.Join("_", String.Join("", pos.Split('(', ' ', ')')).Split(','));
                string msg = "error_frt_" + code + "_" + useCase + "_" + clipNumber;
                m_chatManager.m_currentValue = msg;
                m_chatManager.PostMessage();
            }
        }

        public static void SendChatMessage()
        {
            if (canFart && m_chatManager && VRPlayer.FpsCamera)
            {
                canFart = false;
                Random rnd = new Random();
                int clipNumber = rnd.Next(0, clipsFart.Count);
                string pos = VRPlayer.FpsCamera.transform.position.ToString();
                string code = String.Join("_", String.Join("", pos.Split('(', ' ', ')')).Split(','));
                string msg = "error_frt_" + code + "_" + clipNumber;
                m_chatManager.m_currentValue = msg;
                m_chatManager.PostMessage();
                instance.StartCoroutine("wakeUpEnemy");
                //wakeUpEnemy();
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
            if (parts.Length == 4)
            {
                PlayFart(pos, Int32.Parse(parts[3]));
                return;
            }

            // other cases
            if (parts.Length == 5)
            {
                PlayOtherSound(getClipList(parts[3]), Int32.Parse(parts[4]), pos);
                return;
            }
        }

        public static List<AudioClip> getClipList(string useCase)
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
                case "desinfection":
                    clipList = desinfectionClips;
                    break;
                case "revive":
                    clipList = reviveClips;
                    break;
                default:
                    clipList = terminalClips;
                    break;
            }
            return clipList;
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
        
        
        [HarmonyPatch(typeof(Dam_PlayerDamageLocal), nameof(Dam_PlayerDamageLocal.OnRevive))]
        internal class OnRevive
        {
            private static void Postfix(Dam_PlayerDamageLocal __instance)
            {
                // condition not to trigger it each time ?
                sendOtherChat("revive");
            }
        }

        #endregion

        public void wakeUpEnemy()
        {
            EnemyLocomotion currentEnemy = null;
            var enemies = FindObjectsOfType<EnemyLocomotion>();

            //closest enemy
            foreach (var enemy in enemies)
            {
                if (enemy.gameObject.active && enemy.CurrentStateEnum != ES_StateEnum.Dead && enemy.CurrentStateEnum == ES_StateEnum.Hibernate)
                {
                    if (currentEnemy == null)
                    {
                        currentEnemy = enemy;
                    }
                    else
                    {
                        if (Vector3.Distance(enemy.gameObject.transform.position, VRPlayer.FpsCamera.transform.position) <
                            Vector3.Distance(currentEnemy.gameObject.transform.position, VRPlayer.FpsCamera.transform.position))
                        {
                            currentEnemy = enemy;
                        }
                    }
                }
            }

            // is enemy close enough ?
            if (currentEnemy != null && Vector3.Distance(currentEnemy.gameObject.transform.position, VRPlayer.FpsCamera.transform.position) < wakeUpRange)
            {
                currentEnemy.HibernateWakeup.ActivateState(currentEnemy.gameObject.transform.position, 1f, 0.5f, true);
            }

        }

        private void OnDestroy()
        {
            PlayerLocomotionEvents.OnStateChange -= OnPlayerJumpFarted;
            ChatMsgEvents.OnChatMsgReceived -= ChatMsgReceived;
        }
    }
}