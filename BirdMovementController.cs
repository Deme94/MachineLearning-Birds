using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BirdMovementController : MonoBehaviour
{
    [HideInInspector] public Transform myTransform;
    [HideInInspector] public CharacterController charController;
    private BirdAnimatorController animatorController;
    public Transform lookAt;
    

    public Vector3 direction;                       // Vector de la dirección

    public BoolReference canMove;                   // Si es false, el jugador no se moverá
    public BoolReference canRotate;                 // Si es false, el jugador no rotará

    public FloatReference inputV;                   // Tecla de avance recto

    //public FloatReference runSpeed;                 // Velocidad actual en el modo Run       
    //public FloatReference flySpeed;                 // Velocidad actual en el modo Fly

    public bool auto;                               // Modo automático (aceleración automática)

    public AudioSource playerAudio;                 // Fuente de audio del jugador
    public AudioClip[] movementSounds;              // Sonidos de movimiento del jugador

    private FlyMovement flyMovement;

    void Awake()
    {
        direction = Vector3.zero;
        myTransform = transform;
        charController = GetComponent<CharacterController>();
        animatorController = GetComponent<BirdAnimatorController>();
        flyMovement = GetComponent<FlyMovement>();
    }

    // Use this for initialization
    void Start()
    {
        SwitchToFly();
    }

    // Update is called once per frame
    void Update()
    {
        flyMovement.Move();
    }

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        flyMovement.HandleCollide();
        //transform.parent.GetComponent<AgentePajaroControlador>().AddReward(-0.5f);
        transform.parent.GetComponent<BirdAgentController>().AddReward(-100);
        transform.parent.GetComponent<BirdAgentController>().Reset();
    }

    public void SwitchToFly()
    {
        FastCentre();
        animatorController.SwitchToFly();
    }

    public void FastCentre()
    {
        transform.rotation = lookAt.rotation;
    }
}
