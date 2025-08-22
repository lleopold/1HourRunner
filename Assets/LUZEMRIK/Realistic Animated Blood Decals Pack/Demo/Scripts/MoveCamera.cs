using UnityEngine;
using UnityEngine.Rendering;

namespace LUZEMRIK.BloodDecals
{
    public class MoveCamera : MonoBehaviour
    {
        public BloodDecalAsset _puddles;
        public BloodDecalAsset _splatsFloor;
        public BloodDecalAsset _splatsWall;
        public Color32[] _colors;
        public float _cameraSpeed = 5f;
        public float _cameraSensitivity = 2f;

        private float _xRotation = 0f;
        private float _yRotation = 0f;

        private bool _locked = false;

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
                _locked = !_locked;

            Cursor.lockState = _locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !_locked;

            if (Input.GetKey(KeyCode.W))
                transform.Translate(Vector3.forward * Time.deltaTime * _cameraSpeed);
            if (Input.GetKey(KeyCode.S))
                transform.Translate(Vector3.back * Time.deltaTime * _cameraSpeed);
            if (Input.GetKey(KeyCode.A))
                transform.Translate(Vector3.left * Time.deltaTime * _cameraSpeed);
            if (Input.GetKey(KeyCode.D))
                transform.Translate(Vector3.right * Time.deltaTime * _cameraSpeed);

            _xRotation = _xRotation + Input.GetAxis("Mouse X") * _cameraSensitivity;
            _yRotation = Mathf.Clamp(_yRotation - Input.GetAxis("Mouse Y") * _cameraSensitivity, -89, 89);
            transform.rotation = Quaternion.Euler(_yRotation, _xRotation, 0f);
            GetPressedKey();
            if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hit))
            {
                if (Input.GetKeyDown(KeyCode.Alpha1))
                {
                    BloodDecalManager.Instance.AddDecal(_puddles, _colors[Random.Range(0, _colors.Length)], hit.point, hit.normal, Vector3.one);
                    Debug.Log("Added decal1");
                }
                if (Input.GetKeyDown(KeyCode.Alpha2))
                {
                    BloodDecalManager.Instance.AddDecal(_splatsFloor, _colors[Random.Range(0, _colors.Length)], hit.point, hit.normal, Vector3.one);
                    Debug.Log("Added decal2");
                }
                if (Input.GetKeyDown(KeyCode.Alpha3))
                {
                    BloodDecalManager.Instance.AddDecal(_splatsWall, _colors[Random.Range(0, _colors.Length)], hit.point, hit.normal, Vector3.one);
                    Debug.Log("Added decal3");
                }
            }

            if (Input.GetKeyDown(KeyCode.R))
                BloodDecalManager.Instance.ClearDecals();
        }
        public string GetPressedKey()
        {
            if (Input.GetKeyDown(KeyCode.Alpha1))
                return "1";
            if (Input.GetKeyDown(KeyCode.Alpha2))
                return "2";
            if (Input.GetKeyDown(KeyCode.Alpha3))
                return "3";
            if (Input.GetKeyDown(KeyCode.R))
                return "R";
            return "";
        }
    }
}
