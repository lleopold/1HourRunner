//Description : footstepSystem. Allow to manage player foostep sound depending the surface
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HP.Generics
{
	public class footstepSystem : MonoBehaviour
	{
		public bool				SeeInspector = true;
		public LayerMask		myLayerMask;                           // Ignore specific Layer when physic raycasting is used	
		public Rigidbody		rb;
		public AudioSource		_audio;

		[System.Serializable]
		public class Footsteps
		{
			public List<AudioClip>	footstepSamples;
			public string			MaterialTag = "";
		}

		public List<Footsteps> listFootstepSystem = new List<Footsteps>();


		[System.Serializable]
		public class compareCharacterMagnitude
		{
			public float listTimeBetweenTwoStep = .3f;
			public float listCharacterMangnitude = .4f;
		}


		public List<compareCharacterMagnitude> listCompareMangnitude = new List<compareCharacterMagnitude>();

		private float		_Timer = 0;
		private Vector3		lastPos = new Vector3(0, 0, 0);
		private Vector3		bodyVelocity = new Vector3(0, 0, 0);

		private int			currentFootstepType = 0;
		private int			currentSample = 0;

		private characterMovement charMovement;

		// Use this for initialization
		void Start()
		{
            #region 
            charMovement = GetComponent<characterMovement>(); 
            #endregion
        }

		// Update is called once per frame
		void FixedUpdate()
		{
            #region 
            RaycastHit hit;

            if (Physics.Raycast(rb.transform.position + Vector3.up * .2f, -Vector3.up, out hit, 10, myLayerMask))
            {
                if (charMovement && charMovement.isOnFloor)
                    playFootstep(hit.transform.tag);
                else if (!charMovement)
                    playFootstep(hit.transform.tag);
            } 
            #endregion
        }

		private void playFootstep(string _tag)
		{
            #region 
            if (CheckTag(_tag))
            {
                for (var i = 0; i < listCompareMangnitude.Count; i++)
                {
                    if (bodyVelocity.magnitude > listCompareMangnitude[i].listCharacterMangnitude)
                    {
                        if (!_audio.isPlaying)
                        {
                            float tmpTimeBetweenTwoSteps = listCompareMangnitude[i].listTimeBetweenTwoStep;
                            if (charMovement && charMovement.isRunning)
                                tmpTimeBetweenTwoSteps = listCompareMangnitude[i].listTimeBetweenTwoStep - .2f;

                            if (_Timer == tmpTimeBetweenTwoSteps)
                            {

                                playSound(ChooseSound());
                                _Timer = 0;
                            }
                            else
                            {
                                _Timer = Mathf.MoveTowards(_Timer, tmpTimeBetweenTwoSteps, Time.deltaTime);
                            }
                        }
                    }
                }
            }

            bodyVelocity = (rb.position - lastPos) * 50;
            lastPos = rb.position; 
            #endregion
        }


		private bool CheckTag(string _tag)
		{

            #region 
            for (var i = 0; i < listFootstepSystem.Count; i++)
            {
                if (_tag == listFootstepSystem[i].MaterialTag)
                {
                    currentFootstepType = i;
                    return true;
                }
            }
            return false; 
            #endregion
        }

		private int ChooseSound()
		{
            #region 
            currentSample++;
            currentSample = currentSample % listFootstepSystem[currentFootstepType].footstepSamples.Count;

            return currentSample; 
            #endregion
        }

		private void playSound(int newSample)
		{

            #region 
            if (listFootstepSystem[currentFootstepType].footstepSamples[newSample] != null)
            {
                _audio.clip = listFootstepSystem[currentFootstepType].footstepSamples[newSample];

                int tmpRandomPitch = UnityEngine.Random.Range(-5, 6);
                _audio.pitch = 1 + (tmpRandomPitch * .01f);

                int tmpRandomStartPoint = UnityEngine.Random.Range(0, 9);
                _audio.time = tmpRandomStartPoint * .001f;

                _audio.Play();
            } 
            #endregion
        }

	}

}
