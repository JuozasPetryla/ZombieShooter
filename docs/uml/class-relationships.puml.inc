' Explicit ownership and behavioral relationships reviewed against the source.
ZombieShooter_Core_WalkerCreator ..> ZombieShooter_Core_Walker : creates
ZombieShooter_Core_RunnerCreator ..> ZombieShooter_Core_Runner : creates
ZombieShooter_Core_BruteCreator ..> ZombieShooter_Core_Brute : creates
ZombieShooter_Core_Zombie "0..*" --> "1" ZombieShooter_Core_AttackProfile : Attack
ZombieShooter_Core_EntityFactory "0..*" --> "3" ZombieShooter_Core_ZombieCreator : static ZombieCreators
ZombieShooter_Core_ZombieBuilder ..> ZombieShooter_Core_ZombieArchetype : builds
ZombieShooter_Core_GameSession "0..*" --> "1" ZombieShooter_Core_IGameMode : mode
ZombieShooter_Core_GameSession "0..1" *-- "1" ZombieShooter_Core_EntityFactory
ZombieShooter_Core_WeaponDecorator "0..*" --> "1" ZombieShooter_Core_IWeapon : Inner
ZombieShooter_Core_Player "0..*" --> "2" ZombieShooter_Core_IWeapon : pistol / shotgun
ZombieShooter_Core_Commands_PlayerCommand "0..*" --> "1" ZombieShooter_Core_GameSession : Receiver
ZombieShooter_Core_Commands_UndoCommand "0..*" --> "1" ZombieShooter_Core_GameSession : receiver
ZombieShooter_Core_GameFacade "0..1" *-- "1" ZombieShooter_Core_InputBuffer
ZombieShooter_Core_GameFacade "0..1" *-- "1" ZombieShooter_Core_GameSession
ZombieShooter_Core_Commands_CommandDispatcher "0..*" --> "1" ZombieShooter_Core_GameSession
ZombieShooter_ZombieShooterGame "0..*" --> "1" ZombieShooter_Core_GameFacade : client 1
ZombieShooter_Core_Networking_GameServer "0..*" --> "1" ZombieShooter_Core_GameFacade : client 2
ZombieShooter_ZombieShooterGame "0..*" --> "0..1" ZombieShooter_Core_Networking_NetworkGameClient
ZombieShooter_Core_Networking_NetworkGameClient ..> ZombieShooter_Core_Networking_WireProtocol
ZombieShooter_Core_Networking_GameServer ..> ZombieShooter_Core_Networking_WireProtocol
ZombieShooter_KeyboardMouseAdapter "0..*" --> "1" ZombieShooter_KeyboardMouseDevice : adaptee
ZombieShooter_ControllerAdapter "0..*" --> "1" ZombieShooter_ControllerDevice : adaptee
ZombieShooter_ZombieShooterGame "0..*" --> "2" ZombieShooter_IPlayerInputSource
ZombieShooter_ZombieShooterGame ..> ZombieShooter_HighScores : uses singleton
ZombieShooter_ZombieShooterGame "0..*" --> "1" ZombieShooter_Core_WaveNotification
ZombieShooter_ZombieShooterGame "0..*" --> "1" ZombieShooter_Core_PickupNotification
ZombieShooter_Core_Networking_GameServer "0..*" --> "1" ZombieShooter_Core_WaveNotification
ZombieShooter_Core_Networking_GameServer "0..*" --> "1" ZombieShooter_Core_PickupNotification
ZombieShooter_Core_GameNotification "0..*" --> "1" ZombieShooter_Core_INotificationOutput : Output
ZombieShooter_Core_EntityFactory ..> ZombieShooter_Core_ZombieDirector : constructs templates
ZombieShooter_Core_ZombieDirector "0..*" --> "1" ZombieShooter_Core_ZombieBuilder : builder
ZombieShooter_Core_EntityFactory "0..*" --> "3" ZombieShooter_Core_ZombieArchetype : templates / clones
ZombieShooter_Core_GameSession "0..1" *-- "1" ZombieShooter_Core_Commands_CommandDispatcher
ZombieShooter_Core_GameFacade "0..*" --> "1" ZombieShooter_Core_Commands_CommandDispatcher
ZombieShooter_Core_Commands_CommandDispatcher "0..*" --> "0..*" ZombieShooter_Core_Commands_IGameCommand : pending (max 64) / undo (max 2)
ZombieShooter_Core_GameSession "1" *-- "1..2" ZombieShooter_Core_Player
ZombieShooter_Core_GameSession "1" *-- "0..*" ZombieShooter_Core_Zombie
ZombieShooter_Core_GameSession "1" *-- "0..*" ZombieShooter_Core_Bullet
ZombieShooter_Core_GameSession "1" *-- "0..*" ZombieShooter_Core_Pickup
ZombieShooter_Core_ZombieArchetype "0..*" --> "1" ZombieShooter_Core_AttackProfile : Attack
ZombieShooter_Core_GameFacade "0..*" --> "0..128" ZombieShooter_Core_SequencedEvent
ZombieShooter_Core_GameSession ..> ZombieShooter_Core_GameEvent : publishes
ZombieShooter_Core_GameSession "0..*" --> "0..*" ZombieShooter_Core_IGameObserver : observers (Attach / Detach / Notify)
ZombieShooter_Core_InputBuffer "0..*" --> "0..*" ZombieShooter_Core_PlayerInput
ZombieShooter_Core_Networking_NetworkGameClient "0..*" --> "1" ZombieShooter_Core_Networking_ServerFrame
ZombieShooter_Core_Networking_NetworkGameClient "0..*" --> "1" ZombieShooter_Core_PlayerInput
ZombieShooter_Core_Networking_ServerFrame "0..*" --> "1" ZombieShooter_Core_SessionSnapshot
ZombieShooter_Core_PlayerSnapshot "0..*" --> "2" ZombieShooter_Core_WeaponStats
ZombieShooter_Core_SequencedEvent "0..*" --> "1" ZombieShooter_Core_GameEvent
ZombieShooter_Core_SessionSnapshot "0..*" --> "0..*" ZombieShooter_Core_BulletSnapshot
ZombieShooter_Core_SessionSnapshot "0..*" --> "0..*" ZombieShooter_Core_PickupSnapshot
ZombieShooter_Core_SessionSnapshot "0..*" --> "1..2" ZombieShooter_Core_PlayerSnapshot
ZombieShooter_Core_SessionSnapshot "0..*" --> "0..*" ZombieShooter_Core_SequencedEvent
ZombieShooter_Core_SessionSnapshot "0..*" --> "0..*" ZombieShooter_Core_ZombieSnapshot
ZombieShooter_ZombieShooterGame "0..*" --> "1" ZombieShooter_Core_GameSession
