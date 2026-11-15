namespace Mu.Core.Formats.Bmd;

/// <summary>
/// Las acciones (animaciones) del esqueleto de un personaje. Los números son
/// los que usa el cliente original en <c>PlayerAction</c>; solo se listan las
/// que el cliente reproduce hoy. El índice coincide con el de la acción dentro
/// del BMD, así que se puede pasar directamente a <c>BmdAnimator.Pose</c>.
/// </summary>
public enum PlayerAction
{
    /// <summary>Postura inicial, sin arma.</summary>
    Set = 0,

    /// <summary>Quieto, cuerpo masculino.</summary>
    StopMale = 1,

    /// <summary>Quieto, cuerpo femenino.</summary>
    StopFemale = 2,

    /// <summary>Quieto, con espada.</summary>
    StopSword = 4,

    /// <summary>Quieto, con espada a dos manos.</summary>
    StopTwoHandSword = 5,

    /// <summary>Quieto, con lanza.</summary>
    StopSpear = 6,

    /// <summary>Quieto, con guadaña.</summary>
    StopScythe = 7,

    /// <summary>Quieto, con arco.</summary>
    StopBow = 8,

    /// <summary>Quieto, con ballesta.</summary>
    StopCrossbow = 9,

    /// <summary>Quieto, con bastón.</summary>
    StopWand = 10,

    /// <summary>Caminar, cuerpo masculino.</summary>
    WalkMale = 15,

    /// <summary>Caminar, cuerpo femenino.</summary>
    WalkFemale = 16,

    /// <summary>Caminar con espada.</summary>
    WalkSword = 17,

    /// <summary>Caminar con arco.</summary>
    WalkBow = 21,

    /// <summary>Caminar con bastón.</summary>
    WalkWand = 23,

    /// <summary>Correr, cuerpo masculino.</summary>
    Run = 25,

    /// <summary>Correr con espada.</summary>
    RunSword = 26,

    /// <summary>Correr con espada a dos manos.</summary>
    RunTwoHandSword = 28,

    /// <summary>Correr con arco.</summary>
    RunBow = 30,

    /// <summary>Atacar con el puño.</summary>
    AttackFist = 38,

    /// <summary>Atacar con espada, primer golpe.</summary>
    AttackSwordRight1 = 39,

    /// <summary>Atacar con espada, segundo golpe.</summary>
    AttackSwordRight2 = 40,

    /// <summary>Atacar con espada a dos manos, primer golpe.</summary>
    AttackTwoHandSword1 = 43,

    /// <summary>Atacar con lanza.</summary>
    AttackSpear1 = 46,

    /// <summary>Atacar con arco.</summary>
    AttackBow = 50,

    /// <summary>Atacar con ballesta.</summary>
    AttackCrossbow = 51,

    /// <summary>Lanzar la habilidad de espada, primer nivel.</summary>
    AttackSkillSword1 = 60,

    /// <summary>Lanzar la habilidad de lanza.</summary>
    AttackSkillSpear = 70,

    /// <summary>Saludar, primer gesto.</summary>
    Greeting1 = 187,

    /// <summary>Morir, primera animación.</summary>
    Die1 = 231,

    /// <summary>Morir, segunda animación.</summary>
    Die2 = 232,

    /// <summary>Sentarse.</summary>
    Sit1 = 233,

    /// <summary>Levantarse tras sentarse.</summary>
    Sit2 = 234,
}
