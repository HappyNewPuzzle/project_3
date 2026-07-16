using UnityEngine;

// MainScene에서 전투 배치 위치를 직접 편집할 수 있게 연결하는 Anchor 모음입니다.
public sealed class IdleGuildBattleSceneLayout : MonoBehaviour
{
    [SerializeField] private Transform heroSpawn;
    [SerializeField] private Transform monsterSpawn;
    [SerializeField] private Transform backdropAnchor;
    [SerializeField] private Transform groundAnchor;

    public Transform HeroSpawn => heroSpawn;
    public Transform MonsterSpawn => monsterSpawn;
    public Transform BackdropAnchor => backdropAnchor;
    public Transform GroundAnchor => groundAnchor;

    public bool IsConfigured =>
        heroSpawn != null && monsterSpawn != null && backdropAnchor != null && groundAnchor != null;

    public void Configure(
        Transform hero,
        Transform monster,
        Transform backdrop,
        Transform ground)
    {
        heroSpawn = hero;
        monsterSpawn = monster;
        backdropAnchor = backdrop;
        groundAnchor = ground;
    }
}
