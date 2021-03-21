//using System;
using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Movement : Agent
{
    public float m_Speed = 12f;                 // How fast the tank moves forward and back.
    public float m_TurnSpeed = 0.001f;            // How fast the tank turns in degrees per second.
    public bool isPlayer = false;


    private string m_MovementAxisName;          // The name of the input axis for moving forward and back.
    private string m_TurnAxisName;              // The name of the input axis for turning.

    private float m_MovementInputValue;         // The current value of the movement input.
    private float m_TurnInputValue;             // The current value of the turn input.

    private float distanceToFinish;
    private float distanceToFinishInitial;
    private Vector3 initialPosition;
    private Vector3 tamanioTablero;

    private Rigidbody2D m_Tank_Rb;              // Reference used to move the tank.
    private Rigidbody2D m_Cofre_Rb;

    private GameObject score;


    public override void Initialize()
    {
        m_Tank_Rb = gameObject.GetComponent<Rigidbody2D>();
        m_Cofre_Rb = GameObject.FindGameObjectWithTag("Finish").GetComponent<Rigidbody2D>();
        
        initialPosition = transform.position;
        distanceToFinishInitial = Vector2.Distance(m_Tank_Rb.transform.position, m_Cofre_Rb.transform.position);
        tamanioTablero = GameObject.FindGameObjectWithTag("Tablero").transform.localScale;

        if (isPlayer)
        {
            score = GameObject.Find("ScoreTextPlayer");
        }
        else
        {
            score = GameObject.Find("ScoreTextIA");
        }
        //m_ResetParams = Academy.Instance.EnvironmentParameters;
        //SetResetParameters();
    }

    // Start is called before the first frame update
    void Start()
    {
        // The axes names are based on player number.
        m_MovementAxisName = "Vertical";
        m_TurnAxisName = "Horizontal";
    }


    private void Move()
    {
        m_Tank_Rb.velocity = transform.right * m_MovementInputValue * m_Speed;
    }


    private void Turn()
    {
        // Determine the number of degrees to be turned based on the input, speed and time between frames.
        float rotation = m_TurnInputValue * m_TurnSpeed;
        transform.Rotate(Vector3.back * rotation);
    }


    /// <summary>
    /// Victoria
    /// </summary>
    /// <param name="collision"></param>
    private void OnCollisionEnter2D( Collision2D collision)
    {
        Debug.Log("colision");
        if (collision.collider.tag == "Finish")
        {
            AddReward(5.0f);
            score.GetComponent<ScoreScript>().ScoreValue += 1;
            ResetPosition(false);
        }
        else
        {
            AddReward(-0.02f);
        }
        Debug.Log(GetCumulativeReward());
        //Quiza haya que quitar este endepisode y seguir jugando con el cuadrado en otro sitio. Para seguir sumando puntos.
    }


    /// <summary>
    /// Derrota
    /// </summary>
    /// <param name="collision"></param>
    private void OnTriggerExit2D(Collider2D collision)
    {
        Debug.Log("scape");
        AddReward(-1.0f);
        Debug.Log(GetCumulativeReward());
        score.GetComponent<ScoreScript>().ScoreValue = 0;
        EndEpisode();
    }


    #region ML Agent methods

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(distanceToFinish);

        Vector3 dirToTarget = (m_Cofre_Rb.transform.position - this.transform.position).normalized;

        // Target position in agent frame
        sensor.AddObservation(this.transform.InverseTransformPoint(m_Cofre_Rb.transform.position)); // vec 3

        // Agent velocity in agent frame
        sensor.AddObservation(this.transform.InverseTransformVector(m_Tank_Rb.velocity)); // vec 3

        // Direction to target in agent frame
        sensor.AddObservation(this.transform.InverseTransformDirection(dirToTarget)); // vec 3

        //float velocityAlignment = Vector3.Dot(dirToTarget, m_Aceituna_Rb.velocity);
        //AddReward(0.0001f * velocityAlignment);
    }


    public override void OnActionReceived(float[] vectorAction)
    {
        distanceToFinish = Vector2.Distance(m_Tank_Rb.transform.position, m_Cofre_Rb.transform.position);
        if (distanceToFinish < distanceToFinishInitial)
        //if(distanceToFinish < distanceToFinishInitial/1.1f)
        {
            AddReward(0.01f);   
        }
        else if (distanceToFinish > distanceToFinishInitial)
        {
            AddReward(-0.01f);
        }
        distanceToFinishInitial = distanceToFinish;
        //Debug.Log(GetCumulativeReward());
        //Accion 0 Hacia detras -1 y delante 1
        m_MovementInputValue = Mathf.Clamp(vectorAction[0], -1, 1);
        //Accion 1 Izda -1 Dcha 1
        m_TurnInputValue = Mathf.Clamp(vectorAction[1], -1, 1);
        Move();
        Turn();
        //Debug.Log($"movementinput {m_MovementInputValue} turninput {m_TurnInputValue}");
    }


    public override void OnEpisodeBegin()
    {
        ResetPosition(true);
    }

    private void ResetPosition(bool derrota)
    {
        //Reset Enviroment
        Vector3 nuevaPosicion;
        Collider2D colisionesConMuros;
        if (derrota)
        {
            transform.position = initialPosition;
        }
        m_Tank_Rb.velocity = Vector2.zero;
        //Muevo el cuadrado a una posicion aleatoria que no sea ocupada por un muro.
        do {
            nuevaPosicion = new Vector3(Random.Range(-(tamanioTablero.x / 2) + 0.1f, (tamanioTablero.x / 2) - 0.1f), Random.Range(-(tamanioTablero.y / 2) + 0.1f, (tamanioTablero.y / 2) - 0.1f), tamanioTablero.z);
            //Vemos si colisiona con alguno de los muros (capa 6)*
            colisionesConMuros = Physics2D.OverlapBox(new Vector2(nuevaPosicion.x,nuevaPosicion.y), new Vector2(m_Cofre_Rb.transform.localScale.x, m_Cofre_Rb.transform.localScale.y),0,LayerMask.GetMask(new string[] { "Walls" }));
            if(colisionesConMuros != null )
            {
                DebugDrawBox(new Vector2(nuevaPosicion.x, nuevaPosicion.y), new Vector2(m_Cofre_Rb.transform.localScale.x, m_Cofre_Rb.transform.localScale.y), 0, Color.green, 1000);
            }
            //var cc = Physics2D.OverlapBoxAll(new Vector2(nuevaPosicion.x, nuevaPosicion.y), new Vector2(m_Cuadrado_Rb.transform.localScale.x, m_Cuadrado_Rb.transform.localScale.y), 0);
        } while (colisionesConMuros != null);
        m_Cofre_Rb.transform.position = nuevaPosicion;
        //Actualizo la distancia inicial.
        distanceToFinishInitial = Vector2.Distance(m_Tank_Rb.transform.position, m_Cofre_Rb.transform.position);
        //TODO: Mover el cuadrado a una posicion aleatoria del mapa.
    }


    /// <summary>
    /// Defino las acciones 0 --> adelante detras y 1 --> izda dcha de forma manual con las teclas.
    /// </summary>
    /// <param name="actionsOut"></param>
    public override void Heuristic(float[] actionsOut)
    {
        // Store the value of both input axes.
        m_MovementInputValue = Input.GetAxis(m_MovementAxisName);
        m_TurnInputValue = Input.GetAxis(m_TurnAxisName);
        actionsOut[0] = m_MovementInputValue;
        actionsOut[1] = m_TurnInputValue;       
    }

    #endregion


    void DebugDrawBox(Vector2 point, Vector2 size, float angle, Color color, float duration)
    {

        var orientation = Quaternion.Euler(0, 0, angle);

        // Basis vectors, half the size in each direction from the center.
        Vector2 right = orientation * Vector2.right * size.x / 2f;
        Vector2 up = orientation * Vector2.up * size.y / 2f;

        // Four box corners.
        var topLeft = point + up - right;
        var topRight = point + up + right;
        var bottomRight = point - up + right;
        var bottomLeft = point - up - right;

        // Now we've reduced the problem to drawing lines.
        Debug.DrawLine(topLeft, topRight, color, duration);
        Debug.DrawLine(topRight, bottomRight, color, duration);
        Debug.DrawLine(bottomRight, bottomLeft, color, duration);
        Debug.DrawLine(bottomLeft, topLeft, color, duration);
    }

}
