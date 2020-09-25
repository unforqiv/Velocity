﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using TMPro;

namespace Com.Josh.Velocity
{
    public class Player : MonoBehaviourPunCallbacks, IPunObservable //!
    {
        #region Variables

        public float speed;
        public float sprintModifier;
        public float crouchModifier;
        public float slideModifier;
        public float jumpForce;
        public float lengthOfSlide;

        public int maxHealth;
        public Camera normalCam;
        public GameObject cameraParent;
        public Transform weaponParent;
        public Transform groundDetector;
        public LayerMask ground;

        [HideInInspector] public ProfileData playerProfile;
        public TextMeshPro playerUsername;

        public float slideAmount;
        public float crouchAmount;
        public GameObject standingCollider;
        public GameObject crouchingCollider;


        private Transform ui_healthbar;
        private Text ui_ammo;
        private Text ui_username;

        private Rigidbody rig;

        private Vector3 targetWeaponBobPosition;
        private Vector3 weaponParentOrigin;
        private Vector3 weaponParentCurrentPosition;

        private float movementCounter;
        private float idleCounter;

        private float baseFOV;
        private float sprintFOVModifier = 1.5f;
        private Vector3 origin;

        private int currentHealth;

        private Manager manager;
        private Weapon weapon;

        private bool crouched;
        private bool sliding;
        private float slide_time;
        private Vector3 slide_direction;

        private float aimAngle;

        #endregion

        #region Photon Callbacks

        public void OnPhotonSerializeView(PhotonStream p_stream, PhotonMessageInfo p_message) //!
        {
            if (p_stream.IsWriting)
            {
                p_stream.SendNext((int)(weaponParent.transform.localEulerAngles.x * 100f));
            }
            else
            {
                aimAngle = (int)p_stream.ReceiveNext() / 100f;
            }
        }

        #endregion

        #region Monobehaviour Callbacks
        private void Start()
        {
            manager = GameObject.Find("Manager").GetComponent<Manager>();
            weapon = GetComponent<Weapon>();
            currentHealth = maxHealth;

            cameraParent.SetActive(photonView.IsMine);

            if (!photonView.IsMine) //!
            {
                gameObject.layer = 11;
                standingCollider.layer = 11;
                crouchingCollider.layer = 11;
            }

            baseFOV = normalCam.fieldOfView;
            origin = normalCam.transform.localPosition;

            if(Camera.main)
            {
                Camera.main.enabled = false;
            }

            rig = GetComponent<Rigidbody>();
            weaponParentOrigin = weaponParent.localPosition;
            weaponParentCurrentPosition = weaponParentOrigin;

            if (photonView.IsMine)
            {
                ui_healthbar = GameObject.Find("HUD/Health/Bar").transform;
                ui_ammo = GameObject.Find("HUD/Ammo/Text").GetComponent<Text>();
                ui_username = GameObject.Find("HUD/Username/Text").GetComponent<Text>();

                RefreshHealthBar();
                ui_username.text = Launcher.myProfile.username;

                photonView.RPC("SyncProfile", RpcTarget.All, Launcher.myProfile.username, Launcher.myProfile.level, Launcher.myProfile.xp);
            } 
        }

        private void Update()
        {
            if (!photonView.IsMine)
            {
                RefreshMultiplayerState();
                return;
            }

            //Axles
            float temp_horizontal_move = Input.GetAxisRaw("Horizontal");
            float temp_vertical_move = Input.GetAxisRaw("Vertical");

            //Controls
            bool sprint = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            bool jump = Input.GetKeyDown(KeyCode.Space);
            bool crouch = Input.GetKeyDown(KeyCode.LeftControl) || Input.GetKeyDown(KeyCode.RightControl);
            bool slide = Input.GetKey(KeyCode.LeftControl);
            bool pause = Input.GetKeyDown(KeyCode.Escape); //!

            //States
            bool isGrounded = Physics.Raycast(groundDetector.position, Vector3.down, 0.15f, ground);
            bool isJumping = jump && isGrounded;
            bool isSprinting = sprint && temp_vertical_move > 0 && !isJumping && isGrounded;
            bool isCrouching = crouch && !isSprinting && !isJumping && isGrounded;
            bool isSliding = isSprinting && slide;

            //Crouching
            if (isCrouching)
            {
                photonView.RPC("SetCrouch", RpcTarget.All, !crouched);
            }

            //Jumping
            if (isJumping)
            {
                rig.AddForce(Vector3.up * jumpForce);
            }

            if (Input.GetKeyDown(KeyCode.U)) TakeDamage(100);

            //Head Bob
            if (sliding)
            {
                return;
            }
            else if (temp_horizontal_move == 0 && temp_vertical_move == 0)
            {
                Headbob(idleCounter, 0.025f, 0.025f);
                idleCounter += Time.deltaTime;
                weaponParent.localPosition = Vector3.Lerp(weaponParent.localPosition, targetWeaponBobPosition, Time.deltaTime * 2f);
            }
            else if (!isSprinting)
            {
                Headbob(idleCounter, 0.035f, 0.035f);
                idleCounter += Time.deltaTime * 3f;
                weaponParent.localPosition = Vector3.Lerp(weaponParent.localPosition, targetWeaponBobPosition, Time.deltaTime * 6f);
            }
            else
            {
                Headbob(idleCounter, 0.015f, 0.015f);
                idleCounter += Time.deltaTime * 7f;
                weaponParent.localPosition = Vector3.Lerp(weaponParent.localPosition, targetWeaponBobPosition, Time.deltaTime * 10f);
            }

            //UI Refreshes
            RefreshHealthBar();
            weapon.RefreshAmmo(ui_ammo);
        }

        void FixedUpdate()
        {
            if (!photonView.IsMine)
            {
                return;
            }

            //Axles
            float temp_horizontal_move = Input.GetAxisRaw("Horizontal");
            float temp_vertical_move = Input.GetAxisRaw("Vertical");

            //Controls
            bool sprint = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            bool jump = Input.GetKey(KeyCode.Space);
            bool slide = Input.GetKey(KeyCode.LeftControl);
            bool pause = Input.GetKeyDown(KeyCode.Escape); //!

            //States
            bool isGrounded = Physics.Raycast(groundDetector.position, Vector3.down, 0.1f, ground);
            bool isJumping = jump && isGrounded;
            bool isSprinting = sprint && temp_vertical_move > 0 && !isJumping && isGrounded;
            bool isSliding = isSprinting && slide && !sliding;

            //Pause
            if(pause)
            {
                GameObject.Find("Pause").GetComponent<Pause>().TogglePause();
            }

            if (Pause.paused)
            {
                temp_horizontal_move = 0f;
                temp_vertical_move = 0f;
                sprint = false;
                jump = false;
                pause = false;
                isGrounded = false;
                isJumping = false;
                isSprinting = false;
            }

            //Jumping
            if (isJumping)
            {
                rig.AddForce(Vector3.up * jumpForce);
            }

            //Movement
            Vector3 temp_direction = Vector3.zero;
            float temp_adjustedSpeed = speed;

            if (!sliding)
            {
                temp_direction = new Vector3(temp_horizontal_move, 0, temp_vertical_move);
                temp_direction.Normalize();
                temp_direction = transform.TransformDirection(temp_direction);

                if (isSprinting)
                {
                    if (crouched) photonView.RPC("SetCrouch", RpcTarget.All, false);
                    temp_adjustedSpeed *= sprintModifier;
                }
            }
            else
            {
                temp_direction = slide_direction;
                temp_adjustedSpeed *= slideModifier;
                slide_time -= Time.deltaTime;
                if (slide_time <= 0)
                {
                    sliding = false;
                    weaponParentCurrentPosition += Vector3.up * 0.5f;
                }
            }

            Vector3 temp_targetVelocity = temp_direction * temp_adjustedSpeed * Time.deltaTime;
            temp_targetVelocity.y = rig.velocity.y;
            rig.velocity = temp_targetVelocity;

            //Sliding
            if (isSliding)
            {
                sliding = true;
                slide_direction = temp_direction;
                slide_time = lengthOfSlide;
                weaponParentCurrentPosition += Vector3.down * 0.5f;
            }

            //Camera Stuff
            if (sliding)
            {
                normalCam.fieldOfView = Mathf.Lerp(normalCam.fieldOfView, baseFOV * sprintFOVModifier * 1.25f, Time.deltaTime * 8f);
                normalCam.transform.localPosition = Vector3.Lerp(normalCam.transform.localPosition, origin + Vector3.down * 0.5f, Time.deltaTime * 6f);
            }
            else
            {
                if (isSprinting)
                {
                    normalCam.fieldOfView = Mathf.Lerp(normalCam.fieldOfView, baseFOV * sprintFOVModifier, Time.deltaTime * 8f);
                }
                else
                {
                    normalCam.fieldOfView = Mathf.Lerp(normalCam.fieldOfView, baseFOV, Time.deltaTime * 8f);
                }

                normalCam.transform.localPosition = Vector3.Lerp(normalCam.transform.localPosition, origin, Time.deltaTime * 6f);
            }
        }
        #endregion

        #region Private Methods
        void RefreshMultiplayerState() //!
        {
            float cacheEulY = weaponParent.localEulerAngles.y;

            Quaternion targetRotation = Quaternion.identity * Quaternion.AngleAxis(aimAngle, Vector3.right);
            weaponParent.rotation = Quaternion.Slerp(weaponParent.rotation, targetRotation, Time.deltaTime * 8f);

            Vector3 finalRotation = weaponParent.localEulerAngles;
            finalRotation.y = cacheEulY;

            weaponParent.localEulerAngles = finalRotation;
        }
        void Headbob(float p_z, float p_x_intensity, float p_y_intensity)
        {
            float temp_aim_adjust = 1f;
            if (weapon.isAiming) temp_aim_adjust = 0.1f;
            targetWeaponBobPosition = weaponParentCurrentPosition + new Vector3(Mathf.Cos(p_z) * p_x_intensity *temp_aim_adjust , Mathf.Sin(p_z * 2) * p_y_intensity * temp_aim_adjust, 0);
        }

        [PunRPC]
        void SetCrouch(bool p_state)
        {
            if (crouched == p_state) return;
        }

        void RefreshHealthBar()
        {
            float temp_health_ratio = (float)currentHealth / (float)maxHealth;
            ui_healthbar.localScale = Vector3.Lerp(ui_healthbar.localScale, new Vector3(temp_health_ratio, 1, 1), Time.deltaTime * 8f);
        }

        [PunRPC]
        private void SyncProfile(string p_username, int p_level, int p_xp)
        {
            playerProfile = new ProfileData(p_username, p_level, p_xp);
            playerUsername.text = playerProfile.username;
        }

        #endregion

        #region Public Methods

        public void TakeDamage(int p_damage)
        {
            if(photonView.IsMine)
            {
                if(photonView.IsMine)
                {
                    currentHealth -= p_damage;
                    RefreshHealthBar();
                }
                
                if(currentHealth <= 0)
                {
                    manager.Spawn();
                    PhotonNetwork.Destroy(gameObject);
                }
            }
        }

        #endregion
    }
}
