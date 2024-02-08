using Player;
using UnityEngine;

public class PlayerEffects : MonoBehaviour
{
    [Header("Movement Effects")]
    public ParticleSystem dash;
    public ParticleSystem footprint;
    public ParticleSystem footstepDust;
    public ParticleSystem landingDust;
    public Material activeParticleMat;
    public float delta = 1;
    public float gap = 0.5f;
    int dir = 1;

    Vector3 lastEmit;
   
    CharacterController cc;
    PlayerController pc;

    void Start()
    {
        pc = GetComponent<PlayerController>();
        cc = GetComponent<CharacterController>();
        lastEmit = transform.position;
    }

    public void Update()
    {
        // If distance between last position and current position is bigger than footstep distance
        if ((Vector3.Distance(lastEmit, transform.position) > delta) && cc.isGrounded == true)
        {
            // Calulates a new particle position ()
            var pos = transform.position + (transform.right * gap * dir);
            pos.y = 0.01f;

            // Flips the footstep each time
            dir *= -1;

            // Instantiates a new particle emission parameter and assigns it the calulated postion and current angle
            ParticleSystem.EmitParams ep = new ParticleSystem.EmitParams();
            ep.position = pos;
            ep.rotation = transform.rotation.eulerAngles.y;

            // Emits one footprint particle
            footprint.Emit(ep, 1);

            // Stores the last footprint position
            lastEmit = transform.position;
        }

        if (pc.isMoving == true && cc.isGrounded == true)
        {
            footstepDust.enableEmission = true;
        }
        else
        {
            footstepDust.enableEmission = false;
        }

        if (cc.isGrounded && pc.jumped)
        {
            pc.jumped = false;

            landingDust.Play();
        }

        if (pc.dashed == true)
        {
            dash.enableEmission = true;
        }
        else
        {
            dash.enableEmission = false;
        }
    }
}
