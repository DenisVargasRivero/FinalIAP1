﻿using System;
using System.Linq;
using UnityEngine;
using IA.LineOfSight;
using IA.PathFinding;
using IA.FSM;

public class NPC : MonoBehaviour, IDamageable<Damage, HitResult>
{
    [SerializeField] int health = 100;
    [SerializeField] float AttackRange = 1.5f;
    [SerializeField] LineOfSightComponent sight;
    [SerializeField] Animator anims;
    [SerializeField] PathFindSolver solver;
    [SerializeField] FiniteStateMachine<CommonState> _states;
    [SerializeField] Node _initialTarget;

    public bool LiderUnit = false;

    Action<IDamageable<Damage, HitResult>> setAttackTarget = delegate { };

    public bool IsAlive => health > 0;

    // Start is called before the first frame update
    void Awake()
    {
        if (!anims)
            anims = GetComponent<Animator>();
        if (!solver)
            solver = GetComponent<PathFindSolver>();

        #region StateMachine

        _states = new FiniteStateMachine<CommonState>();

        var idle = GetComponent<IdleState>().SetAnimator(anims).AttachTo(_states, true);

        //Follow Leader
        FollowLeaderState fl = GetComponent<FollowLeaderState>();
        fl.AttachTo(_states);

        //Attack
        AttackState attack = GetComponent<AttackState>();
        attack.OnAttackEnded += AttackCompleted;
        setAttackTarget += attack.SetTarget;
        attack.AttachTo(_states);

        //Move
        MoveToState mv = GetComponent<MoveToState>();
        mv.OnReachedTarget += targetReached;
        mv.findTarget = FindCloserTarget;
        mv.SetAnimator(anims)
          .AttachTo(_states);

        var dead = GetComponent<DeadState>().SetAnimator(anims).AttachTo(_states);

        idle.AddTransition(mv, (cs) => { print("Transitioning!"); })
            .AddTransition(attack)
            .AddTransition(fl)
            .AddTransition(dead, (cs) => { print("Transitioning to Dead from Idle"); });

        mv.AddTransition(idle, (cs) => { print("Transitioning!"); })
          .AddTransition(attack)
          .AddTransition(fl)
          .AddTransition(dead, (cs) => { });

        dead.AddTransition(dead, (cs) => { print("Transitioning"); })
            .AddTransition(idle, (cs) => { });

        #endregion

        if (_states.getCurrentStateType() != CommonState.followLeader && !LiderUnit)
            _states.Feed(CommonState.followLeader);
    }

    // Update is called once per frame
    void Update()
    {
        if (LiderUnit)
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                _states.Feed(CommonState.moveTo);
            }
            //if (Input.GetKeyDown(KeyCode.Alpha0))
            //{
            //    _states.Feed(CommonState.dead);
            //}
            //if (Input.GetKeyDown(KeyCode.Alpha1))
            //{
            //    _states.Feed(CommonState.attack);
            //}
        }

        _states.Update();
    }

    void targetReached()
    {
        Debug.Log("LLegué al objetivo");
        _states.Feed(CommonState.idle);
    }

    public CommonState getCurrentStateType()
    {
        return _states.getCurrentStateType();
    }
    /// <summary>
    /// Encuentra y Evalúa si un Enemigo esta dentro de la línea de visión.
    /// </summary>
    public void FindCloserTarget()
    {
        var posibleTargets = Physics.OverlapSphere(transform.position, sight.range, sight.visibles);
        var closerTarget = posibleTargets
                           .Where(x => x.transform.CompareTag("Human"))
                           .OrderBy(x => Vector3.Distance(transform.position, x.transform.position))
                           .Select(x => GetComponent<IDamageable<Damage, HitResult>>())
                           .FirstOrDefault();

        if (closerTarget != null && sight.IsInSight(closerTarget.transform))
        {
            print("Got you bitch");
            setAttackTarget(closerTarget);
            _states.Feed(CommonState.alert);
        }
    }
    /// <summary>
    /// Cuando la animacion de un ataque fue completado 1 vez.
    /// </summary>
    void AttackCompleted()
    {
        //Por ahora nada we.
    }

    /// <summary>
    /// Esta unidad sufre daño.
    /// </summary>
    /// <param name="inputDamage">Estadísticas del daño recibido.</param>
    /// <returns>El resultado del ataque.</returns>
    public HitResult getHit(Damage inputDamage)
    {
        health -= inputDamage.damageAmmount;

        if (health <= 0)
        {
            health = 0;
            _states.Feed(CommonState.dead);
        }

        return new HitResult();
    }
    /// <summary>
    /// Callback. Se llama cuando esta unidad realiza un ataque sobre otra entidad.
    /// </summary>
    /// <param name="hitResult">El resultado del ataque.</param>
    public void onHit(HitResult hitResult)
    {
        //Si mi target actual está muerto, cambio de estado.
    }
}
