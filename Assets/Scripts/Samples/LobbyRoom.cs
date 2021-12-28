using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LobbyRoom 
{
    public Guid Id { get; set; }

    /*public List<LobbyBattleUserEntity> Players { get; set; } = new List<LobbyBattleUserEntity>();

    //public LobbyBattleUser Owner { get; set; }

    public LobbyBattleUserEntity CurrentUser { get; set; }

    public LobbyBattleUserEntity TargetUser { get; set; }*/
}

public abstract class LobbyBattleUserEntity
{

    public LobbyRoom LobbyRoom { get; set; }

    public List<BattleCharacter> BattleCharacters { get; set; } = new List<BattleCharacter>();

    public LobbyBattleUserEntity Target { get; set; }

    public byte Squad { get; set; }

    public string ConnectionId { get; set; }
}
public class BattleCharacter
{
    //public LobbyBattleUser BattleUser { get; set; }

    //public UserCharacterSquadModel Character { get; set; }
    public int Damage { get; set; }

    public int HealthPoint { get; set; }

    public int Defense { get; set; }

    public double CriticalChance { get; set; }

    public byte Position { get; set; } // состояние героя(заморозка ходов)

    /*public int Initiative => Character.CharacterModel.Initiative;

    public int Range => Character.CharacterModel.Range;

    public int Speed => Character.CharacterModel.Speed;

    public int BaseHealthPoint { get; set; }

    public SkillModel Skill { get; set; }

    public List<BuffEffectModel> DOTCharacter => new List<BuffEffectModel>();*/
}