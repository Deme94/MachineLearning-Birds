using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlyMovement : MonoBehaviour
{
    public float currentSpeed;

    private FloatReference heightOffset;
    private FloatReference marginOffset;

    public float maxSpeed;
    [HideInInspector] public float maxSpeedCopy;
    public float acceleration;
    [HideInInspector] public float accelerationCopy;
    public float efficiency;
    public float rotationSpeed;
    [HideInInspector] public float rotationSpeedCopy;
    public bool runTransition;

    private int flyState;                      // 0 plan, 1 accelerate, -1 deccelerate (animaciones)
    public bool isFalling;
    private float deltaAngleRotationY;
    //private float deltaAngleRotationX;

    public BirdLookAtController lookAt;
    BirdMovementController controller;
    BirdAnimatorController animator;
    public GameObject airTrail;
    public GameObject vfxCrash;
    public GameObject vfxFeatherTwirl;
    public AudioSourceSplitScreen3D audioSourceFly;
    public AudioSourceSplitScreen3D audioSourceChirp;
    public AudioSourceSplitScreen3D audioSourceCrash;
    public AudioSourceSplitScreen3D audioSourceTwirls;
    public AudioClip[] damageChirpClips;
    public AudioClip[] chirpClips;

    void Awake()
    {
        controller = GetComponent<BirdMovementController>();
        animator = GetComponent<BirdAnimatorController>();
        heightOffset = lookAt.upOffset;
        marginOffset = lookAt.rightOffset;
        maxSpeedCopy = maxSpeed;
        accelerationCopy = acceleration;
        rotationSpeedCopy = rotationSpeed;
    }

    void OnEnable()
    {
        currentSpeed = 1;
    }

    public void Move()
    {
        if (controller.canRotate.Value)
        {
            Rotate();
        }
        if (controller.canMove.Value)
        {
            FlyMove();
        }
        controller.charController.Move(controller.direction * Time.deltaTime);  // SE MUEVE
    }

    private void Rotate()
    {
        // La velocidad de rotacion aumenta o disminuye en funcion de la velocidad actual
        if(currentSpeed > 0)
            rotationSpeed = (maxSpeedCopy/currentSpeed) * rotationSpeedCopy;
        rotationSpeed = Mathf.Clamp(rotationSpeed, rotationSpeedCopy, rotationSpeedCopy * 2);
        // Si esta quieto
        if (currentSpeed == 0)
        {
            controller.transform.rotation = Quaternion.Slerp(controller.transform.rotation,
                Quaternion.Euler(-2, controller.transform.eulerAngles.y, 0), 5 * Time.deltaTime);

            marginOffset.Value = Mathf.Lerp(marginOffset.Value, 0f, 2 * Time.deltaTime);
            heightOffset.Value = Mathf.Lerp(heightOffset.Value, 0f, 2 * Time.deltaTime);

            lookAt.SmoothCentre();
        }

        // Si esta moviendose
        else
        {
            // Calculo de los angulos que hay entre el pajaro y el lookAt
            deltaAngleRotationY = Mathf.DeltaAngle(controller.transform.eulerAngles.y, controller.lookAt.eulerAngles.y);
            //deltaAngleRotationX = Mathf.DeltaAngle(controller.transform.eulerAngles.x, controller.lookAt.eulerAngles.x);
            // Se realizan las rotaciones del pajaro relativas al angulo de giro
            if (Mathf.Abs(deltaAngleRotationY) < 45)
            {
                controller.transform.rotation = Quaternion.Slerp(controller.transform.rotation,
                    Quaternion.Euler(controller.lookAt.eulerAngles.x, controller.lookAt.localEulerAngles.y,
                    -deltaAngleRotationY * 0.8f * rotationSpeed / 1.5f), rotationSpeed * Time.deltaTime);
            }
            else
            {
                controller.transform.rotation = Quaternion.Slerp(controller.transform.rotation,
                    Quaternion.Euler(-5, controller.lookAt.eulerAngles.y,
                    -deltaAngleRotationY * rotationSpeed / 1.5f), rotationSpeed * Time.deltaTime);
            }

            // Efecto de camara al curvar durante el vuelo

            marginOffset.Value = Mathf.Lerp(marginOffset.Value, deltaAngleRotationY / 32, 4f * Time.deltaTime);
            marginOffset.Value = Mathf.Clamp(marginOffset.Value, -2f, 2f);
            //marginOffset.Value = Mathf.Lerp(marginOffset.Value, 0f, 2*Time.deltaTime);
            heightOffset.Value = Mathf.Lerp(heightOffset.Value, deltaAngleRotationY / 32, 0.5f * Time.deltaTime);
            heightOffset.Value = Mathf.Clamp(heightOffset.Value, -0.2f, 0.2f);
            //heightOffset.Value = Mathf.Lerp(heightOffset.Value, 0f, 2*Time.deltaTime);
        }
        
    }

    private void FlyMove()
    {
        animator.UpdateFly();

        float angleX = Mathf.DeltaAngle(0, controller.transform.eulerAngles.x);
        float inclinationMagnitude = Mathf.Sin(controller.transform.eulerAngles.x * Mathf.PI / 180);
        float calculatedSpeed;
        // Se calcula la velocidad calculada con proporciones distintas a la inclinacion en funcion de si baja o sube
        if (angleX > 0)
        {
            calculatedSpeed = maxSpeed * (1 + 2 * inclinationMagnitude);
        }
        else
        {
            calculatedSpeed = maxSpeed * (1 + inclinationMagnitude / 5);
        }

        // MODO PLANEO
        if (controller.inputV.Value == 0)
        {
            if (currentSpeed > 1)
            {
                Glide();
            }
            // SI BAJA
            if (angleX > 0)
            {
                currentSpeed = Mathf.Lerp(currentSpeed,
                    calculatedSpeed, inclinationMagnitude * Time.deltaTime);
            }
            // SI SUBE
            else
            {
                if (currentSpeed < 1)
                {
                    Accelerate();
                }
                currentSpeed = Mathf.Lerp(currentSpeed,
                        0.5f, (0.1f - inclinationMagnitude) * (1 - efficiency) * Time.deltaTime);
            }
        }
        // ACELERA
        else if (controller.inputV.Value > 0)
        {
            // SI BAJA
            if (angleX > 0)
            {
                // Usa la aceleracion del ave si la inclinacion no supera 30 grados y puede aumentar la velocidad
                if (angleX < 30 && calculatedSpeed > currentSpeed)
                {
                    Accelerate();
                    currentSpeed = Mathf.Lerp(currentSpeed,
                    calculatedSpeed, acceleration * Time.deltaTime);
                }
                // Usa la aceleracion gravitatoria si la inclinacion supera los 30 grados
                else
                {
                    Glide();
                    currentSpeed = Mathf.Lerp(currentSpeed,
                        calculatedSpeed, inclinationMagnitude * Time.deltaTime);
                }
            }
            // SI SUBE
            else
            {
                // Acelera si va por debajo de la velocidad alcanzable
                if (currentSpeed < calculatedSpeed)
                {
                    Accelerate();
                    currentSpeed = Mathf.Lerp(currentSpeed,
                        calculatedSpeed, acceleration * Time.deltaTime);
                }
                // No acelera si va mas rapido de la velocidad alcanzable (va con inercia), decelera naturalmente
                else
                {
                    Glide();
                    currentSpeed = Mathf.Lerp(currentSpeed,
                        0.5f, (0.1f - inclinationMagnitude) * (1 - efficiency) * Time.deltaTime);
                }
            }
        }
        // DECELERA
        else
        {
            // SI BAJA
            if (angleX > 0)
            {
                // Frena si la inclinacion no supera 30 grados
                if (angleX < 30)
                {
                    if (currentSpeed < 6)
                    {
                        Deccelerate();
                    }
                    else
                    {
                        Glide();
                    }
                    currentSpeed = Mathf.Lerp(currentSpeed,
                    0.5f, acceleration * 0.1f * Time.deltaTime);
                }
                // Usa la aceleracion gravitatoria si la inclinacion supera los 30 grados
                else
                {
                    Glide();
                    currentSpeed = Mathf.Lerp(currentSpeed,
                        calculatedSpeed, inclinationMagnitude * Time.deltaTime);
                }
            }
            // SI SUBE
            else
            {
                // Frena sin condiciones
                if (currentSpeed < 6)
                {
                    Deccelerate();
                }
                currentSpeed = Mathf.Lerp(currentSpeed,
                    0.5f, acceleration * 0.1f * Time.deltaTime);
            }
        }

        controller.direction.Set(0, 0, currentSpeed);

        // Transformamos la direccion de local a world space (relativa al transform del player)
        controller.direction = controller.transform.TransformDirection(controller.direction);
    }

    public void HandleCollide()
    {
        audioSourceCrash.Play();
        PlayDamageSound();
        Destroy(Instantiate<GameObject>(vfxCrash, transform.position+Vector3.up*0.3f, Quaternion.identity), 2);
        Deccelerate();
        controller.canMove.Value = false;
        controller.direction = -controller.transform.forward * 4;
        controller.StartCoroutine(CollisionCoroutine());
    }

    private IEnumerator CollisionCoroutine()
    {
        currentSpeed = 0;
        yield return new WaitForSeconds(0.5f);
        currentSpeed = 0.5f;
        controller.canMove.Value = true;
        Glide();
    }

    private void Glide()
    {
        if (flyState != 0)
        {
            airTrail.SetActive(true);
            animator.Glide();
            audioSourceFly.Clip = controller.movementSounds[1];
            audioSourceFly.Loop = true;
            audioSourceFly.Volume = 0.1f;
            audioSourceFly.Pitch = 0.5f;
            audioSourceFly.Play();
            flyState = 0;
        }
    }
    private void Accelerate()
    {
        if (flyState != 1)
        {
            airTrail.SetActive(false);
            animator.Accelerate();
            audioSourceFly.Clip = controller.movementSounds[0];
            audioSourceFly.Loop = true;
            audioSourceFly.Volume = 0.07f;
            audioSourceFly.Pitch = 2f;
            audioSourceFly.Play();
            flyState = 1;
        }
    }
    private void Deccelerate()
    {
        if (flyState != -1)
        {
            airTrail.SetActive(false);
            animator.Deccelerate();
            audioSourceFly.Clip = controller.movementSounds[0];
            audioSourceFly.Loop = true;
            audioSourceFly.Volume = 0.17f;
            audioSourceFly.Pitch = 3f;
            audioSourceFly.Play();
            flyState = -1;
        }
    }

    public void PlayDamageSound()
    {
        audioSourceChirp.Clip = damageChirpClips[Random.Range(0, damageChirpClips.Length)];
        audioSourceChirp.Play();
    }

    public void PlayChirp()
    {
        audioSourceChirp.Clip = chirpClips[Random.Range(0, chirpClips.Length)];
        audioSourceChirp.Play();
    }
}

