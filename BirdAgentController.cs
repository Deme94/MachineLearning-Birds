using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using UnityEngine;

public class BirdAgentController : Agent
{
    public enum Heuristica
    {
        Joystick,
        CurvaBezier
    };
    public Heuristica heuristica;
    public bool normal;

    private BirdMovementController controller;
    private FlyMovement fly;
    private BirdLookAtController lookAtController;
    public Transform nextCheckpoint;

    // Bezier attributes
    float totalDistanceToTarget;
    Vector3 startPoint;
    Vector3 p1;

    // Agent
    private Vector3 startPosition;
    private Vector3 checkpointDir;
    private Vector3 myDir;
    private Vector3 myLookAtDir;
    private float startDistanceToTarget;
    private float distanceToTarget;
    private float heightDifference;
    private float dotProduct_LookAt;
    private float dotProduct_Body;
    private float angleY_LookAt;
    private float angleX_LookAt;
    private float angleY_Body;
    private float angleX_Body;
    private readonly float maxSpeed = 7 * (1 + 2 * Mathf.Sin(40 * Mathf.PI / 180)); // Maxima velocidad a la que puede spawnear un pajaro
    private float startEpisodeTime;
    private bool reset;
    private Transform checkpoint1;

    // Start is called before the first frame update
    public override void Initialize()
    {
        controller = GetComponentInChildren<BirdMovementController>();
        fly = GetComponentInChildren<FlyMovement>();
        lookAtController = GetComponentInChildren<BirdLookAtController>();
        startPosition = controller.transform.position;

        checkpoint1 = GameObject.Find("Sphere (1)").transform;
    }

    public override void OnEpisodeBegin()
    {
        reset = false;
        startEpisodeTime = Time.time;
        SetNextCheckpoint(checkpoint1);
        GetComponent<BirdRaceController>().nextCheckpoint = checkpoint1;

        float randomX = Random.Range(-40, 89);
        float randomY = Random.Range(0, 360);

        // SPAWNS ALEATORIOS
        controller.myTransform.position = startPosition + Random.insideUnitSphere * 40;
        controller.myTransform.rotation = Quaternion.Euler(randomX, randomY, 0);
        lookAtController.myTransform.rotation = controller.myTransform.rotation;
        fly.currentSpeed = Random.Range(4, maxSpeed);

        checkpointDir = nextCheckpoint.position - controller.myTransform.position;
        myDir = lookAtController.myTransform.forward;
        startDistanceToTarget = checkpointDir.sqrMagnitude;
        distanceToTarget = startDistanceToTarget;
        dotProduct_LookAt = Vector3.Dot(checkpointDir.normalized, myDir);

        GenerateBezierCurve();
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        checkpointDir = nextCheckpoint.position - controller.myTransform.position;
        myDir = lookAtController.myTransform.forward;
        myLookAtDir = lookAtController.myTransform.forward;

        // Calculo de observaciones
        distanceToTarget = checkpointDir.sqrMagnitude;
        heightDifference = (controller.myTransform.position.y - nextCheckpoint.position.y);
        dotProduct_LookAt = Vector3.Dot(checkpointDir.normalized, myLookAtDir);
        dotProduct_Body = Vector3.Dot(checkpointDir.normalized, myDir);
        angleY_LookAt = Vector3.SignedAngle(myLookAtDir, checkpointDir, Vector3.up);
        angleX_LookAt = Mathf.DeltaAngle(0, lookAtController.myTransform.eulerAngles.x);
        angleY_Body = Vector3.SignedAngle(myDir, checkpointDir, Vector3.up);
        angleX_Body = Mathf.DeltaAngle(0, controller.myTransform.eulerAngles.x);

        // ---------------------------------------- CASOS DE ESTUDIO ----------------------------------------------
        // Caso 1.Solo posiciones - no aprende(7 variables)
        //sensor.AddObservation(controller.myTransform.position); // Posicion global de pajaro
        //sensor.AddObservation(nextCheckpoint.position); // Posicion global del checkpoint pajaro
        //sensor.AddObservation(fly.currentSpeed); // Velocidad actual del pajaro

        // Caso 2. Posiciones globales (10 variables)
        //sensor.AddObservation(controller.myTransform.position); // Posicion global de pajaro
        //sensor.AddObservation(nextCheckpoint.position); // Posicion global del checkpoint pajaro
        //sensor.AddObservation(myLookAtDir); // Direccion del pajaro
        //sensor.AddObservation(fly.currentSpeed); // Velocidad actual del pajaro

        //// Caso 3. Posiciones relativas (7 variables)
        //sensor.AddObservation(checkpointDir); // Posicion del checkpoint respecto al pajaro, o direccion hacia el checkpoint
        //sensor.AddObservation(myLookAtDir); // Direccion del pajaro
        //sensor.AddObservation(fly.currentSpeed); // Velocidad actual del pajaro

        // Caso 4. No normalizado o simplificado (5 variables)
        //sensor.AddObservation(distanceToTarget); // Distancia del pajaro al objetivo
        //sensor.AddObservation(heightDifference); // Altura del pajaro respecto al objetivo
        //sensor.AddObservation(angleAxisY); // Angulo horizontal (eje Y) del pajaro respecto al objetivo
        //sensor.AddObservation(angleX); // Inclinacion del pajaro (eje X)
        //sensor.AddObservation(fly.currentSpeed); // Velocidad actual del pajaro

        // Caso 5. Normalizado (5 variables)
        //sensor.AddObservation(distanceToTarget / 6800); // Distancia del pajaro al objetivo
        //sensor.AddObservation(heightDifference / 40); // Altura del pajaro respecto al objetivo
        //sensor.AddObservation(angleY_LookAt / 180); // Angulo horizontal (eje Y) del pajaro respecto al objetivo
        //sensor.AddObservation(angleX_LookAt / 90); // Inclinacion del pajaro (eje X)
        //sensor.AddObservation(fly.currentSpeed / maxSpeed); // Velocidad actual del pajaro
        
        // Caso optimo (7 variables)
        sensor.AddObservation(distanceToTarget / 6800); // Distancia del pajaro al objetivo
        sensor.AddObservation(heightDifference / 40); // Altura del pajaro respecto al objetivo
        sensor.AddObservation(angleY_LookAt / 180); // Angulo horizontal (eje Y) del pajaro respecto al objetivo
        sensor.AddObservation(angleX_LookAt / 90); // Inclinacion del pajaro (eje X)
        sensor.AddObservation(angleY_Body / 180); // Angulo horizontal (eje Y) del cuerpo del pajaro respecto al objetivo
        sensor.AddObservation(angleX_Body / 90); // Inclinacion del cuerpo del pajaro (eje X)
        sensor.AddObservation(fly.currentSpeed / maxSpeed); // Velocidad actual del pajaro
    }

    public override void OnActionReceived(float[] vectorAction)
    {
        // Acciones recibidas
        lookAtController.rotarInclinacion.Value = Mathf.Clamp(vectorAction[0], -1, 1) * 120 * Time.deltaTime; // Arriba-abajo
        lookAtController.rotarDireccion.Value = Mathf.Clamp(vectorAction[1], -1, 1) * 125 * Time.deltaTime; // Izq-Der
        controller.inputV.Value = 1;       
    }

    private void FixedUpdate()
    {
        // ENTRENAMIENTO NORMALIZADO
        //AddReward(-0.002f); // Pierde recompensa con el tiempo (así intentará llegar en el menor tiempo posible y sin perder tiempo)        
        //if (dotProduct_LookAt > 0)
        //    AddReward(0.1f * dotProduct_LookAt); // Gana más recompensa cuanto más orientado al objetivo esté        
        //AddReward(0.001f * (fly.currentSpeed / fly.maxSpeed)); // Fomenta que el pajaro vaya a la maxima velocidad posible

        //// POSITIVAS Y NEGATIVAS
        //AddReward(-0.002f); // Pierde recompensa con el tiempo (así intentará llegar en el menor tiempo posible y sin perder tiempo)        
        //AddReward(0.1f * dotProduct); // Gana recompensa cuanto más orientado al objetivo esté y la pierde si va en sentido contrario
        //AddReward(0.001f * (fly.currentSpeed / fly.maxSpeed)); // Fomenta que el pajaro vaya a la maxima velocidad posible

        // VELOCIDAD ALTA
        //AddReward(-0.002f); // Pierde recompensa con el tiempo (así intentará llegar en el menor tiempo posible y sin perder tiempo)        
        //if (dotProduct > 0)
        //    AddReward(0.1f * dotProduct); // Gana más recompensa cuanto más orientado al objetivo esté        
        //AddReward(0.1f * (fly.currentSpeed / fly.maxSpeed)); // Fomenta que el pajaro vaya a la maxima velocidad posible

        // RECOMPENSAS ALTAS (ESCALADAS)
        //AddReward(-0.002f * 20); // Pierde recompensa con el tiempo (así intentará llegar en el menor tiempo posible y sin perder tiempo)        
        //if (dotProduct > 0)
        //    AddReward(0.1f * dotProduct * 20); // Gana más recompensa cuanto más orientado al objetivo esté        
        //AddReward(0.001f * (fly.currentSpeed / fly.maxSpeed) * 20); // Fomenta que el pajaro vaya a la maxima velocidad posible

        // POCAS RECOMPENSAS (MENOS VARIEDAD)
        //AddReward(-0.002f); // Pierde recompensa con el tiempo (así intentará llegar en el menor tiempo posible y sin perder tiempo)    

        // ENTRENAMIENTO CORTO
        //AddReward(-0.004f); // Pierde recompensa con el tiempo (así intentará llegar en el menor tiempo posible y sin perder tiempo)        
        //if (dotProduct > 0)
        //    AddReward(0.1f * dotProduct); // Gana más recompensa cuanto más orientado al objetivo esté        

        // ENTRENAMIENTO OPTIMO
        AddReward(-0.02f); // Pierde recompensa con el tiempo (así intentará llegar en el menor tiempo posible y sin perder tiempo) 
        if (dotProduct_LookAt > 0)
            AddReward(0.005f * dotProduct_LookAt); // Gana más recompensa cuanto más orientado al objetivo esté
        if (dotProduct_Body > 0)
            AddReward(0.005f * dotProduct_Body); // Gana más recompensa cuanto más orientado al objetivo esté
        AddReward(0.001f * (fly.currentSpeed / fly.maxSpeed)); // Fomenta que el pajaro vaya a la maxima velocidad posible

        if (reset || Time.time > startEpisodeTime + 10)
        {
            EndEpisode();
        }
    }

    public void Reset()
    {
        reset = true;
    }

    // Algoritmo de IA tradicional basado en la curva Bezier
    public override void Heuristic(float[] actionsOut)
    {
        if (heuristica == Heuristica.Joystick)
        {
            // Arriba-abajo
            actionsOut[0] = -Input.GetAxis("Camera Y");
            // Izquierda-derecha
            actionsOut[1] = Input.GetAxis("Camera X");
        }
        else
        {
            float distanceToTarget = Vector3.SqrMagnitude(nextCheckpoint.position - controller.myTransform.position);
            Vector3 directionToTarget = (nextCheckpoint.position - controller.myTransform.position).normalized;

            float angleAxisY = Vector3.SignedAngle(lookAtController.myTransform.forward, directionToTarget, Vector3.up);
            float t = (totalDistanceToTarget - distanceToTarget) / totalDistanceToTarget;

            float bezierY = CalculateQuadraticBezierPoint(t, startPoint, p1, nextCheckpoint.position).y;

            // Si estoy por encima de la curva
            if (controller.myTransform.position.y > bezierY)
            {
                // Si estoy a menos de 5 metros sobre la curva
                if (controller.myTransform.position.y - bezierY < 5)
                {
                    float idealAngle = (controller.myTransform.position.y - bezierY) / 5 * lookAtController.maxAngleX;
                    // Empiezo a enderezar, calculo el ángulo ideal del pajaro en función de lo cerca
                    // que está de la curva. 
                    // Si la separación es de 5 metros o más, el ángulo del pájaro será el máximo permitido.
                    // Si la separación es de 0 metros, el ángulo del pájaro deberá ser 0.
                    if (lookAtController.myTransform.eulerAngles.x > idealAngle && lookAtController.myTransform.eulerAngles.x < 90)
                    {
                        actionsOut[0] = -1;     // SUBE
                    }
                    else
                    {
                        actionsOut[0] = 1;      // BAJA
                    }
                }
                else
                {
                    actionsOut[0] = 1;      // BAJA
                }
            }
            // Si estoy por debajo de la curva
            else
            {
                // Si estoy a menos de 2 metros bajo la curva
                if (controller.myTransform.position.y - bezierY > -2)
                {
                    float idealAngle = ((bezierY - controller.myTransform.position.y) / 2) * lookAtController.minAngleX;
                    // Empiezo a enderezar, calculo el ángulo ideal del pajaro en funcion de lo cerca
                    // que está de la curva. 
                    // Si la separación es de 1 metro o más, el ángulo del pájaro será el máximo permitido.
                    // Si la separación es de 0 metros, el ángulo del pájaro deberá ser 0.
                    if (lookAtController.myTransform.eulerAngles.x < 360 + idealAngle && lookAtController.myTransform.eulerAngles.x > 270)
                    {
                        actionsOut[0] = 1;     // BAJA
                    }
                    else
                    {
                        actionsOut[0] = -1;     // SUBE
                    }
                }
                else
                {
                    actionsOut[0] = -1;     // SUBE
                }
            }

            // EL TARGET ESTA A LA DERECHA
            if (angleAxisY > 0)
            {
                actionsOut[1] = 1;     // GIRA A LA DERECHA
            }
            // EL TARGET ESTA A LA IZQUIERDA
            else
            {
                actionsOut[1] = -1;    // GIRA A LA IZQUIERDA
            }

            if (distanceToTarget < 0.5)
            {
                SetNextCheckpoint(nextCheckpoint.GetComponent<CheckpointController>().nextCheckpoint);
            }
        }
    }

    private void GenerateBezierCurve()
    {
        float distanceY = Mathf.Abs(controller.myTransform.position.y - nextCheckpoint.position.y);
        totalDistanceToTarget = Vector3.SqrMagnitude(nextCheckpoint.position - controller.myTransform.position);
        startPoint = controller.myTransform.position;
        if (controller.myTransform.position.y > nextCheckpoint.position.y)
        {
            p1 = controller.myTransform.position - Vector3.up * distanceY;
        }
        else
            p1 = controller.myTransform.position + Vector3.up * distanceY;
    }
    public Vector3 CalculateQuadraticBezierPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2)
    {
        float u = 1 - t;
        Vector3 position = u * u * p0;
        position += 2 * u * t * p1;
        position += t * t * p2;
        return position;
    }

    public void SetNextCheckpoint(Transform checkpoint)
    {
        nextCheckpoint = checkpoint;
    }
}
