﻿using System;
using Lidgren.Network;
using System.Threading;
//using System.Diagnostics; --unused

namespace SAR_Server_App
{
    class Match
        //sendType 73 == doodad destroy
    {
        private NetPeerConfiguration config;
        public NetServer server;
        public Player[] playerList;
        public Thread updateThread;
        private int matchSeed1, matchSeed2, matchSeed3; //figure out later
        private int REFRESH_RATE = 10;
        private bool MATCH_STARTED;
        private bool MATCH_FULL;
        public double startTimer = 0;
        int prevTime = DateTime.Now.Second; //redo start times later

        //these get to go at some point, or never. I'm quite lazy.
        public bool DEBUG_ENABLED;
        public bool ANOYING_DEBUG1;

        public Match(int port, string ip, bool db, bool annoying)
        {
            MATCH_STARTED = false;
            MATCH_FULL = false;
            playerList = new Player[64];
            startTimer = 120.00;
            updateThread = new Thread(serverUpdateThread);
            DEBUG_ENABLED = db;
            ANOYING_DEBUG1 = annoying;
            config = new NetPeerConfiguration("BR2D");
            config.EnableMessageType(NetIncomingMessageType.ConnectionLatencyUpdated);
            config.EnableMessageType(NetIncomingMessageType.ConnectionApproval);
            config.PingInterval = 22f;
            config.LocalAddress = System.Net.IPAddress.Parse(ip); //"192.168.1.198" DEFAULT
            config.Port = port; //42896 DEFAULT
            server = new NetServer(config);
            updateThread.Start();
            server.Start();

            NetIncomingMessage msg;
            while (true)
            {
                while ((msg = server.ReadMessage()) != null)
                {
                    switch (msg.MessageType)
                    {
                        case NetIncomingMessageType.Data:
                            HandleMessage(msg);
                            break;
                        case NetIncomingMessageType.StatusChanged:
                            Logger.Header("~-- { Status Change} --~");
                            switch (msg.SenderConnection.Status)
                            {
                                case NetConnectionStatus.Connected:
                                    Logger.Success($"a client has connected! connection address: {msg.SenderConnection}");
                                    NetOutgoingMessage acceptMsgg = server.CreateMessage();
                                    acceptMsgg.Write((byte)0);
                                    acceptMsgg.Write(true);
                                    server.SendMessage(acceptMsgg, msg.SenderConnection, NetDeliveryMethod.ReliableSequenced);
                                    break;
                                case NetConnectionStatus.Disconnected:
                                    Logger.Warn($"{msg.SenderConnection} has disconnected...");
                                    break;
                                case NetConnectionStatus.Disconnecting:
                                    Logger.Warn($"{msg.SenderConnection} is disconnecting...");
                                    break;
                            }
                            break;
                        case NetIncomingMessageType.ConnectionApproval:
                            Logger.Header("<< Incoming Connection >>");
                            //Console.WriteLine("Incoming Connection! Wowie!");
                            string clientKey = msg.ReadString();
                            if (clientKey == "flwoi51nawudkowmqqq")
                            {
                                if (!MATCH_STARTED)
                                {
                                    Logger.Success("Connection Allowed");
                                    //Console.WriteLine("Connection is from correct version. Allowing");
                                    msg.SenderConnection.Approve();
                                }
                                else
                                {
                                    msg.SenderConnection.Deny($"The match has already begun, sorry!");
                                }

                            }
                            else { msg.SenderConnection.Deny($"Your client version key is incorrect.\n\nYour version key: {clientKey}"); Logger.Failure("Client Connected with wrong key..."); }
                            break;
                        case NetIncomingMessageType.DebugMessage:
                            Logger.DebugServer(msg.ReadString());
                            break;
                        case NetIncomingMessageType.WarningMessage:
                        case NetIncomingMessageType.ErrorMessage:
                            Logger.Failure("EPIC BLUNDER! " + msg.ReadString());
                            break;
                        case NetIncomingMessageType.ConnectionLatencyUpdated:
                            //look I don't know why, but yes sending a message here does include the type ConnectionLatencyUpdated.
                            Logger.Header($"ping from {msg.SenderConnection}");
                            NetOutgoingMessage pingBack = server.CreateMessage();
                            pingBack.WriteTime(true);
                            pingBack.Write("hi lol");
                            server.SendMessage(pingBack, msg.SenderConnection, NetDeliveryMethod.ReliableUnordered);
                            break;
                        default:
                            Logger.Failure("Unhandled type: " + msg.MessageType);
                            break;
                    }
                    //Console.WriteLine("\n[[Loop End]]\n\n");
                    server.Recycle(msg);
                }
                Thread.Sleep(REFRESH_RATE); //maybe this should be lower. but I am not too sure
            }
        }

        private void serverUpdateThread() //where most things get updated...
        {
            while (true)
            {
                if (playerList[playerList.Length-1] != null && !MATCH_FULL)
                {
                    MATCH_FULL = true;
                    Logger.Basic("Match seems to be full!");
                }

                //check the count down timer
                if (!MATCH_STARTED) { checkStartTime(); }

                //Console.WriteLine("Sending Update to players...");
                sendEveryonePlayer();


                


                Thread.Sleep(10); // ~1ms delay
            }
        }

        private void sendEveryonePlayer()
        {
            NetOutgoingMessage playerUpdate = server.CreateMessage();
            playerUpdate.Write((byte)11); // Header -- Basic Update Info

            //Find Length of Actual Players
            //Logger.Warn($"playerlist length: {(byte)playerList.Length}");
            //playerUpdate.Write((byte)playerList.); // Ammount of times to loop (for amount of players, you know?

            for (byte i = 0; i < playerList.Length; i++)
            {
                if (playerList[i] == null)
                { 
                    playerUpdate.Write(i); // Ammount of times to loop (for amount of players, you know?
                    //Logger.Header($"list length: {i}");
                    //Logger.Warn($"sendEveryonePlayer i value: {i}");
                    break;
                }
                else { continue; }
            }


            for (int i = 0; i < playerList.Length; i++)
            {
                if (playerList[i] != null)
                {
                    playerUpdate.Write(playerList[i].assignedID); // may be able to simplfiy by just writing "I"
                    playerUpdate.Write(playerList[i].mouseAngle);
                    //playerUpdate.Write((sbyte)4); //hard coded. has something to do with mouse angle? -- if added back use reverted version!
                    playerUpdate.Write(playerList[i].position_X); //REALLY need to fix this...
                    playerUpdate.Write(playerList[i].position_Y); //really need to fix this as well...
                }
                else { break; }
            }
            server.SendToAll(playerUpdate, NetDeliveryMethod.ReliableSequenced);
        }
        private void checkStartTime()
        {
            if (startTimer > 0)
            {
                if ((prevTime < DateTime.Now.Second) || (prevTime == 59 && DateTime.Now.Second == 0))
                {
                    if ((startTimer % 2) < 1)
                    {
                        Logger.Basic($"seconds until start: {startTimer}");
                        //Logger.Basic($"Previous Second: {prevTime}");
                        //Logger.Basic($"Current Second: {DateTime.Now.Second}");
                    }
                    startTimer -= 1;
                    prevTime = DateTime.Now.Second;
                }
            }
            else if (startTimer == 0)
            {
                //this is so it waits an extra second
                if ((prevTime < DateTime.Now.Second) || (prevTime == 59 && DateTime.Now.Second == 0))
                {
                    sendStartGame();
                    MATCH_STARTED = true;
                }
            }
        }
        private void sendStartGame()
        {
            if (DEBUG_ENABLED) { Logger.Warn("Sending game begin to all clients!"); }
            NetOutgoingMessage startMsg = server.CreateMessage();
            startMsg.Write((byte)6); //Header
            startMsg.Write(20f); //x1
            startMsg.Write(30f); //y1
            startMsg.Write(40f); //x2
            startMsg.Write(50f); //y2
            startMsg.Write((byte)1); //b4 -- one loop
            startMsg.Write((short)30); //readInt16
            startMsg.Write((short)600); // readInt16 -- percentage
            startMsg.Write((byte)1);//b5 -- one loop 
            startMsg.Write((short)120); //snowtime... something tells me reading ints is purposefully complicated...
            startMsg.Write((short)600);

            //Send message out
            server.SendToAll(startMsg, NetDeliveryMethod.ReliableUnordered);
            /*Console.WriteLine("Case 97 Activated!");
            //must send a message with a byte starting with six
            NetOutgoingMessage servMsg = server.CreateMessage();
            servMsg.Write((byte)6); //header of 6
            servMsg.Write(2f); //x2
            servMsg.Write(3f); //y2
            servMsg.Write(5f); //x3
            servMsg.Write(6f); //y3//

            servMsg.Write((byte)1); //byte 1
            short e = 32420;
            //servMsg.Write(e);
            servMsg.Write(e);
            servMsg.Write(e + 1);
            servMsg.Write(e + 2);
            servMsg.Write(e + 3);
            servMsg.Write(1);
            servMsg.Write(e);
            servMsg.Write(e + 1);
            servMsg.Write(e + 2);
            servMsg.Write(e + 3);
            server.SendToAll(servMsg, NetDeliveryMethod.ReliableOrdered);*/
        }

        //unused
        private void sendStartTime()
        {
            NetOutgoingMessage stmsg = server.CreateMessage();
            stmsg.Write((byte)43);
            stmsg.Write(startTimer);
        }


        // End of Update Thread

        //Start of Handle Message Routine
        

        private void HandleMessage(NetIncomingMessage msg)
        {
            //Stopwatch watch = new Stopwatch();
            //watch.Start();
            byte b = msg.ReadByte();
            if (DEBUG_ENABLED) {
                if (b != 14)
                {
                    Logger.Header($"Byte : {b}");
                } }

            switch (b)
            {
                // Request Authentication
                case 1:
                    Console.WriteLine($"Authentication Request! -- Sender: {msg.SenderConnection}");
                    sendAuthToPlayer(msg.SenderConnection);
                    break;

                case 3:
                    Console.WriteLine($"Ready message from -- {msg.SenderConnection} -- Read Player Info");
                    //short pID = short.MaxValue;
                    for (short i = 0; i < playerList.Length; i++)
                    {
                        if (playerList[i] == null)
                        {
                            //pID = i;
                            ulong steamID = msg.ReadUInt64(); //steamID64- this is from ME! :D
                            string readName = msg.ReadString(); //player's name from steam; this is also from me! :D
                            short charID = msg.ReadInt16(); // Character/Avatar ID
                            short umbrellaID = msg.ReadInt16(); // Umbrella ID
                            short gravestoneID = msg.ReadInt16(); // Gravestone ID
                            short deathExplosionID = msg.ReadInt16(); // Death Explosion ID
                            short[] emoteIDs = { msg.ReadInt16(), msg.ReadInt16(), msg.ReadInt16(), msg.ReadInt16(), msg.ReadInt16(), msg.ReadInt16(), }; // Emote ID
                            short hatID = msg.ReadInt16(); // Hat ID
                            short glassesID = msg.ReadInt16(); // Glasses ID
                            short beardID = msg.ReadInt16(); // Beard ID
                            short clothesID = msg.ReadInt16(); // Clothes ID
                            short meleeID = msg.ReadInt16(); // MeleeWeaponID
                            byte skinIndexID = msg.ReadByte(); // GunSkinByGunID
                            short[] skinShorts = new short[skinIndexID];
                            byte[] skinValues = new byte[skinIndexID];
                            for (byte l = 0; l < skinIndexID; l++)
                            {
                                skinShorts[l] = msg.ReadInt16();
                                skinValues[l] = msg.ReadByte();
                            }
                            //short skinShort = msg.ReadInt16(); // indexInJSONFileList
                            //byte skinKey = msg.ReadByte(); // keyValuePair.Value

                            playerList[i] = new Player(i, charID, umbrellaID, gravestoneID, deathExplosionID, emoteIDs, hatID, glassesID, beardID, clothesID, meleeID, skinIndexID, skinShorts, skinValues);
                            playerList[i].sender = msg.SenderConnection;
                            playerList[i].myName = readName;
                            switch (steamID)
                            {
                                case 76561198384352240: //m
                                    playerList[i].isMod = true;
                                    break;
                                case 76561198218282413:
                                    playerList[i].isMod = true;
                                    break;
                                case 76561198162222086:
                                    playerList[i].isMod = true;
                                    break;
                                case 76561198323046172:
                                    playerList[i].isMod = true;
                                    break;
                                default:
                                    playerList[i].isFounder = true;
                                    break;
                            }
                            sendClientMatchInfo2Connect(i, msg.SenderConnection);
                            break;
                        }
                    }

                    //no longer needed, but is useful.
                    if (DEBUG_ENABLED)
                    {
                        for (int i = 0; i < playerList.Length; i++)
                        {
                            if (playerList[i] != null)
                            {
                                Logger.Basic($"Player ID For Match: {playerList[i].assignedID}");
                                Logger.Basic($"Avatar/Character ID: {playerList[i].avatarID}");
                                Logger.Basic($"Umbrella ID: {playerList[i].umbrellaID}");
                                Logger.Basic($"Gravestone ID: {playerList[i].gravestoneID}");
                                Logger.Basic($"Death Explosion ID: {playerList[i].deathExplosionID}");
                                Logger.Basic($"Emote ID: {playerList[i].emoteIDs}");
                                Logger.Basic($"Hat ID: {playerList[i].hatID}");
                                Logger.Basic($"Glasses ID: {playerList[i].glassesID}");
                                Logger.Basic($"Beard ID: {playerList[i].beardID}");
                                Logger.Basic($"Clothes ID: {playerList[i].clothesID}");
                                Logger.Basic($"Melee Weapon ID: {playerList[i].meleeID}");
                                Logger.Basic($"Gun-Skin-by-Index-ID: {playerList[i].gunSkinIndexByIDAmmount}");
                                //Logger.Basic($"Unsure 1: {playerList[i].UNKNOWN_BYTE}");
                                //Logger.Basic($"Unsure 2: {playerList[i].UNKNOWN_DATA}");
                            }
                            else { break; }
                        }
                    }
                    break;

                case 5:
                    Logger.Header($"<< sending {msg.SenderEndPoint} player characters... >>");
                    sendPlayerCharacters();
                    break;
                case 7:
                    short plr = getPlayerID(msg.SenderConnection);
                    NetOutgoingMessage sendEject = server.CreateMessage();
                    sendEject.Write((byte)8);
                    sendEject.Write(playerList[plr].assignedID);
                    sendEject.Write(playerList[plr].position_X);
                    sendEject.Write(playerList[plr].position_Y);
                    sendEject.Write(true);
                    server.SendToAll(sendEject, NetDeliveryMethod.ReliableSequenced);
                    break;

                case 14:
                    float mAngle = msg.ReadFloat(); //before I changed this to float it was int16 idrk if that matters...
                    float actX = msg.ReadFloat();
                    float actY = msg.ReadFloat();
                    byte currentwalkMode = msg.ReadByte();

                    for (short i = 0; i < playerList.Length; i++)
                    {
                        if ((playerList[i] != null))
                        {
                            if (playerList[i].sender == msg.SenderConnection)
                            {
                                playerList[i].position_X = actX;
                                playerList[i].position_Y = actY;
                                playerList[i].mouseAngle = mAngle;
                                playerList[i].currWalkMode = currentwalkMode;
                                break;
                            }
                        }
                        else
                        {
                            break;
                        }
                    }
                    //annoying debug section
                    if (ANOYING_DEBUG1)
                    {
                        Logger.Warn($"Mouse Angle as a Short: {mAngle}");
                        Logger.Warn($"playerX: {actX}");
                        Logger.Warn($"playerY: {actY}");
                        Logger.Basic($"player WalkMode: {currentwalkMode}");
                    } 
                    break;
                case 16:

                    short weaponID = msg.ReadInt16(); //short -- WeaponId
                    byte slotIndex = msg.ReadByte();//byte -- slotIndex
                    float aimAngle = msg.ReadFloat();//float (myver) -- aimAngle
                    float spawnPoint_X = msg.ReadFloat();//float -- spawnPoint.X
                    float spawnPoint_Y = msg.ReadFloat();//float -- spawnPoint.Y
                    bool shotPointValid = msg.ReadBoolean();//bool -- shotPointValid
                    bool didHitADestruct = msg.ReadBoolean();//bool -- didHitDestructible
                    short destructCollisionPoint_X = 0; //short -- destructCollisionPoint.X
                    short destructCollisionPoint_Y = 0; //short -- destruct.CollisionPoint.y
                    if (didHitADestruct)
                    {
                        destructCollisionPoint_X = msg.ReadInt16();
                        destructCollisionPoint_Y = msg.ReadInt16(); // I think this is meant for the server but whatevs
                    }
                    short attackID = msg.ReadInt16();//short -- attackID
                    byte sendProjectileAnglesArrayLength = msg.ReadByte();//byte -- projectileAngles.Length


                    NetOutgoingMessage plrShot = server.CreateMessage();
                    plrShot.Write((byte)17);
                    plrShot.Write(getPlayerID(msg.SenderConnection)); //playerID of shot
                    plrShot.Write((ushort)1); //playerPing figure this out later I don't give a darn right now!
                    plrShot.Write(weaponID); //weaponID from shot
                    plrShot.Write(slotIndex); //slotIndex
                    plrShot.Write(attackID); //attackID
                    plrShot.Write(aimAngle); //angle
                    plrShot.Write(spawnPoint_X);
                    plrShot.Write(spawnPoint_Y);
                    plrShot.Write(shotPointValid);
                    plrShot.Write(sendProjectileAnglesArrayLength);
                    if (sendProjectileAnglesArrayLength > 0)
                    {
                        for (int i = 0; i < sendProjectileAnglesArrayLength; i++)
                        {
                            plrShot.Write(msg.ReadFloat());
                            plrShot.Write(msg.ReadInt16());
                            plrShot.Write(msg.ReadBoolean());
                        }
                    }
                    server.SendToAll(plrShot, NetDeliveryMethod.ReliableSequenced);

                    /* using this as a base for later...
                    short weaponID = msg.ReadInt16(); //short -- WeaponId
                    byte slotIndex = msg.ReadByte();//byte -- slotIndex
                    float aimAngle = msg.ReadFloat();//float (myver) -- aimAngle
                    float spawnPoint_X = msg.ReadFloat();//float -- spawnPoint.X
                    float spawnPoint_Y = msg.ReadFloat();//float -- spawnPoint.Y
                    bool shotPointValid = msg.ReadBoolean();//bool -- shotPointValid
                    bool didHitADestruct = msg.ReadBoolean();//bool -- didHitDestructible
                    short destructCollisionPoint_X = 0; //short -- destructCollisionPoint.X
                    short destructCollisionPoint_Y = 0; //short -- destruct.CollisionPoint.y
                    if (didHitADestruct)
                    {
                        destructCollisionPoint_X = msg.ReadInt16();
                        destructCollisionPoint_Y = msg.ReadInt16();
                    }
                    short attackID = msg.ReadInt16();//short -- attackID
                    byte sendProjectileAnglesArrayLength = msg.ReadByte();//byte -- projectileAngles.Length
                    float projectileInstAngle = 0;
                    short projectileID = 0;
                    bool didHit = false;
                    if (sendProjectileAnglesArrayLength > 0)
                    {
                        for (byte i = 0; i < sendProjectileAnglesArrayLength; i++)
                        {
                            projectileInstAngle = msg.ReadFloat();
                            Logger.Basic($"Projectile ID {i}.Angle: {projectileInstAngle}");
                            projectileID = msg.ReadInt16();
                            Logger.Basic($"Projectile ID {i}.ReadID: {projectileID}");
                            didHit = msg.ReadBoolean();
                            Logger.Basic($"Projectile ID {i}.didHit: {didHit}");
                        }
                    }

                    if (DEBUG_ENABLED)
                    {
                        Logger.Basic($"WeaponID: {weaponID}\nSlotIndex: {slotIndex}\naimAngle: {aimAngle}\nSpawn X: {spawnPoint_X}\nSpawn Y: {spawnPoint_Y}\nValid? {shotPointValid}" +
                            $"\nhitDestruct: {didHitADestruct}\nDestruct X: {destructCollisionPoint_X}\nDestruct Y: {destructCollisionPoint_Y}\nAttack ID: {attackID}\nArrayLength: {sendProjectileAnglesArrayLength}\nProjectile Inst Angle: {projectileInstAngle}\nProjectile ID: {projectileID}\nDid Hit? {didHit}");
                    }*/
                    break;
                case 21:
                    //Writes 21 > Write(int:lootID) > Write(byte:slotIndex)
                    
                    short item = (short)msg.ReadInt32();
                    byte index = msg.ReadByte();
                    if (DEBUG_ENABLED) { Logger.Basic($"Loot ID: {item}\nSlotIndex: {index}"); }
                    for (int i = 0; i < playerList.Length; i++)
                    {
                        if (playerList[i] != null)
                        {
                            if (playerList[i].sender == msg.SenderConnection)
                            {
                                switch (index)
                                {
                                    case 0:
                                        playerList[i].equip1 = item;
                                        playerList[i].equip1_rarity = 0;
                                        break;
                                    case 1:
                                        playerList[i].equip2 = item;
                                        playerList[i].equip2_rarity = 0;
                                        break;
                                    default:
                                        Logger.Failure($"Well something went wrong with the index... index: {index}");
                                        break;
                                }
                                NetOutgoingMessage testMessage = server.CreateMessage();
                                testMessage.Write((byte)22);
                                testMessage.Write(playerList[i].assignedID); //player
                                testMessage.Write((int)item); // Thing
                                //testMessage.Write(playerList[i].equip1);
                                testMessage.Write(index);
                                testMessage.Write((byte)4); //Forced Rarity
                                server.SendToAll(testMessage, NetDeliveryMethod.ReliableUnordered);
                            }
                        }
                        else { break; }
                    }
                    break;


                case 25:
                    NetOutgoingMessage allchatmsg = server.CreateMessage();
                    allchatmsg.Write((byte)26);
                    allchatmsg.Write(getPlayerID(msg.SenderConnection)); //ID of player who sent msg
                    allchatmsg.Write(msg.ReadString());
                    allchatmsg.Write(false);
                    server.SendToAll(allchatmsg, NetDeliveryMethod.ReliableUnordered);
                    break;

                //clientSentSelectedSlot
                case 27:
                    serverSendSlotUpdate(msg.SenderConnection, msg.ReadByte());
                    break;

                case 29: //Received Reloading
                    NetOutgoingMessage sendReloadMsg = server.CreateMessage();
                    sendReloadMsg.Write((byte)30);
                    sendReloadMsg.Write(getPlayerID(msg.SenderConnection)); //sent ID
                    sendReloadMsg.Write(msg.ReadInt16()); //weapon ID
                    sendReloadMsg.Write(msg.ReadByte()); //slot ID
                    server.SendToAll(sendReloadMsg, NetDeliveryMethod.ReliableOrdered);
                    break;
                case 92: //Received DONE reloading
                    NetOutgoingMessage doneReloading = server.CreateMessage();
                    doneReloading.Write((byte)93);
                    doneReloading.Write(getPlayerID(msg.SenderConnection)); //playerID
                    server.SendToAll(doneReloading, NetDeliveryMethod.ReliableOrdered); //yes it's that simple
                    break;
                //figure it out.
                case 36:
                    serverSendBeganGrenadeThrow(msg);
                    break;
                case 38:
                    serverSendGrenadeThrowing(msg);
                    break;

                case 55: //Entering a hamball
                    short vehPlr = getPlayerArrayIndex(msg.SenderConnection);
                    short enteredVehicleID = msg.ReadInt16();
                    NetOutgoingMessage enterVehicle = server.CreateMessage();
                    enterVehicle.Write((byte)56);
                    enterVehicle.Write(playerList[vehPlr].assignedID); //sent ID
                    enterVehicle.Write(enteredVehicleID); //vehicle ID
                    enterVehicle.Write(playerList[vehPlr].position_X); //X
                    enterVehicle.Write(playerList[vehPlr].position_Y); //Y
                    playerList[vehPlr].vehicleID = enteredVehicleID;
                    server.SendToAll(enterVehicle, NetDeliveryMethod.ReliableOrdered);
                    break;

                //clientSendExitHamsterball
                case 57:
                    short vehPlrEx = getPlayerArrayIndex(msg.SenderConnection);
                    NetOutgoingMessage exitVehicle = server.CreateMessage();
                    exitVehicle.Write((byte)58);
                    exitVehicle.Write(playerList[vehPlrEx].assignedID); //sent ID
                    exitVehicle.Write(msg.ReadInt16()); //vehicle ID
                    exitVehicle.Write(playerList[vehPlrEx].position_X); //X
                    exitVehicle.Write(playerList[vehPlrEx].position_Y); //Y
                    playerList[vehPlrEx].vehicleID = -1;
                    server.SendToAll(exitVehicle, NetDeliveryMethod.ReliableOrdered); //yes it's that simple
                    break;
                //clientSendVehicleHitPlayer
                case 60:
                    serverSendVehicleHitPlayer(msg);
                    break;

                //clientSendVehicleHitWall
                case 62:
                    serverSendPlayerHamsterballBounce(msg);
                    Logger.Failure("and the hamsterball message has finished... so now I get to write!");
                    break;

                case 64:
                    short vehShotWepID = msg.ReadInt16();//WeaponID, ID of the weapon that shot the vehicle
                    short targetedVehicleID = msg.ReadInt16(); //targetVehicleID, vehicle that was shot at
                    short optionalProjectileID = msg.ReadInt16();
                    if (DEBUG_ENABLED)
                    {
                        Logger.Header($"Someone has shot a hamsterball...");
                        Logger.Basic($"Weapon ID: {vehShotWepID}\nTargeted Vehicle ID: {targetedVehicleID}\nProjectile ID: {optionalProjectileID}");
                    }
                    NetOutgoingMessage ballHit = server.CreateMessage();
                    ballHit.Write( (byte)65 );
                    ballHit.Write(getPlayerID(msg.SenderConnection));
                    ballHit.Write(targetedVehicleID);
                    ballHit.Write( (byte)0 );
                    ballHit.Write(optionalProjectileID);
                    server.SendToAll(ballHit, NetDeliveryMethod.ReliableUnordered);
                    break;

                //clientSendPlayerEmote
                case 66:

                    //Send Back a response
                    short ePlayerID = getPlayerID(msg.SenderConnection);
                    short ePlayerIndex = getPlayerArrayIndex(msg.SenderConnection);
                    NetOutgoingMessage emoteMsg = server.CreateMessage();
                    emoteMsg.Write((byte)67); //Header
                    emoteMsg.Write(ePlayerID); //obviously ID
                    emoteMsg.Write(msg.ReadInt16()); //Read emote id to then send to everyone!
                    server.SendToAll(emoteMsg, NetDeliveryMethod.ReliableUnordered);

                    //update player info rq
                    playerList[ePlayerIndex].position_X = msg.ReadFloat();
                    playerList[ePlayerIndex].position_Y = msg.ReadFloat();
                    break;
                case 72:
                    short descXthing = msg.ReadInt16(); //x
                    short descYthing = msg.ReadInt16(); //y
                    NetOutgoingMessage descBroke = server.CreateMessage();
                    descBroke.Write((byte)73);
                    descBroke.Write((short)31257); //it just wants a short idk why
                    descBroke.Write(descXthing);
                    descBroke.Write(descYthing);
                    //descBroke.Write(msg.ReadInt16()); //xSpot
                    //descBroke.Write(msg.ReadInt16()); //ySpot -- next read will be optionalProjectileID
                    descBroke.Write((short)1);
                    descBroke.Write(descXthing);
                    descBroke.Write(descYthing);
                    descBroke.Write((byte)0);
                    descBroke.Write((byte)1);
                    descBroke.Write((short)4);

                    server.SendToAll(descBroke, NetDeliveryMethod.ReliableUnordered);
                    break;
                //client sent attack wind up
                case 74:
                    Logger.testmsg("\nplease note this occurred!!! attack windup. well that's rare...\n-- please note this\n");
                    serverSendAttackWindUp(msg);
                    break;
                //client sent attack wind down
                case 75:
                    Logger.testmsg("\nplease note this occurred!!! attack winddown. well that's even rarer...\n-- please note this\n");
                    serverSendAttackWindDown(getPlayerID(msg.SenderConnection), msg.ReadInt16());
                    break;

                case 90:
                    NetOutgoingMessage plrCanceled = server.CreateMessage();
                    plrCanceled.Write((byte)91);
                    plrCanceled.Write(getPlayerID(msg.SenderConnection));
                    server.SendToAll(plrCanceled, NetDeliveryMethod.ReliableSequenced);
                    break;

                case 97:
                    NetOutgoingMessage callbackmsg = server.CreateMessage();
                    callbackmsg.Write((byte)97);
                    server.SendMessage(callbackmsg, msg.SenderConnection, NetDeliveryMethod.UnreliableSequenced);
                    break;

                default:
                    Logger.missingHandle(b.ToString());
                    break;

            }

            //watch.Stop();
            //Logger.Header($"time taken: {watch.Elapsed}");
            //this stopwatch is very much not needed.
        }
        //message 1 -> send 2
        private void sendAuthToPlayer(NetConnection client)
        {
            NetOutgoingMessage acceptMsg = server.CreateMessage();
            acceptMsg.Write((byte)2);
            acceptMsg.Write(true);
            server.SendMessage(acceptMsg, client, NetDeliveryMethod.ReliableOrdered);
            Logger.Basic($"Server sent client: {client} their accept message!");
        }

        //message 3 > << message 4 >> --still needs working
        private void sendClientMatchInfo2Connect(short sendingID, NetConnection receiver)
        {
            // send a message back to the connecting player... \\
            NetOutgoingMessage mMsg = server.CreateMessage();
            mMsg.Write((byte)4); //Header (doesn't matter)
            mMsg.Write(sendingID); // Assigned Player ID

            // TODO : Make Seed Randomized...
            mMsg.Write(351301); //seed 1 -- int32
            mMsg.Write(5328522); //seed 2 -- int32 
            mMsg.Write(9037281); //seed 3 -- int32 
            // TODO : MAKE SEED RANDOMIZED

            mMsg.Write(startTimer); //time at which game will start [double] 
            mMsg.Write("yerhAGJ"); // match UUID -- string -- MAKE THIS RANDOMIZED OR SOMETHING IDK
            mMsg.Write("solo");
            mMsg.Write((float)0); //x -- No clue? -- maybe has to do with flightpath
            mMsg.Write((float)0); //y
            mMsg.Write((float)8000); //x2
            mMsg.Write((float)8000); //y2 -- No clue -- flight start and end points ig

            mMsg.Write((byte)0); // amount of times to loop through thing ig but I skipped out. something with gallery targets
            mMsg.Write((byte)0); // that gallery target's score or whatever but I don't give a darn

            server.SendMessage(mMsg, receiver, NetDeliveryMethod.ReliableOrdered);
            //msg.SenderConnection.Disconnect("Currently Testing Stuff! Please come back later!");
        }

        //message 5 > send10
        private void sendPlayerCharacters()
        {
            NetOutgoingMessage sendPlayerPosition = server.CreateMessage();
            sendPlayerPosition.Write((byte)10);
            for (byte i = 0; i < playerList.Length; i++)
            {
                if (playerList[i] == null)
                {
                    sendPlayerPosition.Write(i); // Ammount of times to loop (for amount of players, you know?
                    break;
                }
            }

            // loop through the list of all of the players in the match \\
            for (short i = 0; i < playerList.Length; i++)
            {
                if (playerList[i] != null)
                {
                    Logger.Header($"Sending << playerList[{i}] >>");
                    //For Loop Start // this byte may be a list of all players. I'm not sure though!
                    sendPlayerPosition.Write(i); //num4 / myAssignedPlayerID? [SHORT]
                    sendPlayerPosition.Write(playerList[i].avatarID); //charIndex [SHORT]
                    sendPlayerPosition.Write(playerList[i].umbrellaID); //umbrellaIndex [SHORT]
                    sendPlayerPosition.Write(playerList[i].gravestoneID); //gravestoneIndex [SHORT]
                    sendPlayerPosition.Write(playerList[i].deathExplosionID); //explosionIndex [SHORT]
                    for (int j = 0; j < playerList[i].emoteIDs.Length; j++)
                    {
                        Console.WriteLine("loop ammount: " + j);
                        sendPlayerPosition.Write(playerList[i].emoteIDs[j]); //emoteIndex [SHORT]
                    }
                    sendPlayerPosition.Write(playerList[i].hatID); //hatIndex [SHORT]
                    sendPlayerPosition.Write(playerList[i].glassesID); //glassesIndex [SHORT]
                    sendPlayerPosition.Write(playerList[i].beardID); //beardIndex [SHORT]
                    sendPlayerPosition.Write(playerList[i].clothesID); //clothesIndex [SHORT]
                    sendPlayerPosition.Write(playerList[i].meleeID); //meleeIndex [SHORT]

                    //Really Confusing Loop
                    sendPlayerPosition.Write(playerList[i].gunSkinIndexByIDAmmount);
                    for (byte l = 0; l < playerList[i].gunSkinIndexByIDAmmount; l++)
                    {
                        sendPlayerPosition.Write(playerList[i].gunSkinKeys[l]); //Unknown Key
                        sendPlayerPosition.Write(playerList[i].gunSkinIndexes[l]); //Unknown Value
                    }

                    //Positioni?
                    sendPlayerPosition.Write(playerList[i].position_X);
                    sendPlayerPosition.Write(playerList[i].position_Y);

                    //sendPlayerPosition.Write((float)508.7); //x2
                    //sendPlayerPosition.Write((float)496.7); //y2
                    sendPlayerPosition.Write(playerList[i].myName); //playername

                    sendPlayerPosition.Write(playerList[i].currenteEmote); //num 6 - int16 -- I think this is the emote currently in use. so... defualt should be none/ -1
                    sendPlayerPosition.Write(playerList[i].equip1); //equip -- int16
                    sendPlayerPosition.Write(playerList[i].equip2); //equip2 - int16
                    sendPlayerPosition.Write(playerList[i].equip1_rarity); // equip rarty byte
                    sendPlayerPosition.Write(playerList[i].equip2_rarity); // equip rarity 2 -- byte
                    sendPlayerPosition.Write(playerList[i].curEquipIndex); // current equip index -- byte
                                                                           //sendPlayerPosition.Write((short)12); //num8 -- something with emotes?
                    /* 0 -- Default; 4-- Clap; 10 -- Russian; 11- Laugh; 
                     */
                    sendPlayerPosition.Write(playerList[i].isDev); //isDev
                    sendPlayerPosition.Write(playerList[i].isMod); //isMod
                    sendPlayerPosition.Write(playerList[i].isFounder); //isFounder
                    sendPlayerPosition.Write((short)450); //accLvl -- short
                    sendPlayerPosition.Write((byte)1); //b6 -- not too sure, but normal byte
                    sendPlayerPosition.Write((short)25); //list of something gets added...

                }
                else { Console.WriteLine($"playerList[{i}] is null. Breaking... "); break; }// break out of loop
            }
            Console.WriteLine("Sending playerPositions packet!!!");
            server.SendToAll(sendPlayerPosition, NetDeliveryMethod.ReliableSequenced); // CHANGED FROM BOTTOM TO THIS IDK WHAT IT DOES
                                                                                       //server.SendMessage(sendPlayerPosition, msg.SenderConnection, NetDeliveryMethod.UnreliableSequenced);
        }

        //got 27 > send 28
        private void serverSendSlotUpdate(NetConnection snd, byte sentSlot)
        {
            Player plr = playerList[getPlayerArrayIndex(snd)];
            plr.activeSlot = sentSlot;

            NetOutgoingMessage msg = server.CreateMessage();
            msg.Write((byte)28);
            msg.Write(plr.assignedID);
            msg.Write(sentSlot);
            server.SendToAll(msg, NetDeliveryMethod.ReliableUnordered);
        }

        //got 36 > send 37
        private void serverSendBeganGrenadeThrow(NetIncomingMessage message)
        {
            NetOutgoingMessage msg = server.CreateMessage();
            msg.Write((byte)37);
            msg.Write(getPlayerID(message.SenderConnection));
            msg.Write(message.ReadInt16());
            server.SendToAll(msg, NetDeliveryMethod.ReliableSequenced);
        }
        //got 38 > send 39
        private void serverSendGrenadeThrowing(NetIncomingMessage message)
        {
            NetOutgoingMessage msg = server.CreateMessage();
            msg.Write((byte)39);
            for (byte i = 0; i < 3; i++)
            {
                msg.Write(message.ReadFloat()); //x
                msg.Write(message.ReadFloat()); //y
            }
            short grenadeID = message.ReadInt16();
            msg.Write(grenadeID);
            msg.Write(grenadeID);//likely needs to be unique. not sure how. maybe just make the server have its own counter
            server.SendToAll(msg, NetDeliveryMethod.ReliableSequenced);
        }

        //send 61
        private void serverSendVehicleHitPlayer(NetIncomingMessage message)
        {
            Logger.Basic($"Target Player ID: {message.ReadInt16()}\nSpeed: {message.ReadFloat()}");
            //It isn't that the bottom code doesn't work, it is just that the "correct" speed value needs to be found.

            /*
            //client SENDS this
            NetOutgoingMessage netOutgoingMessage = GameServerManager.netClient.CreateMessage();
            netOutgoingMessage.Write(60);
            netOutgoingMessage.Write(targetPlayerID);
            netOutgoingMessage.Write(speed);

            NetOutgoingMessage vehicleHit = server.CreateMessage();
            vehicleHit.Write((byte)61); //Message #61
            vehicleHit.Write(getPlayerID(message.SenderConnection)); //player who hit
            vehicleHit.Write(message.ReadInt16()); //player who GOT hit

            

            //client gets this
            short fromPlayerID2 = msg.ReadInt16();
            short toPlayerID2 = msg.ReadInt16();
            bool didKillPlayer = msg.ReadBoolean();
            short fromVehicleIndex = msg.ReadInt16();
            short num33 = msg.ReadInt16();
            byte optionalTargetVehicleHP = 0;
            if (num33 >= 0)
            {
                optionalTargetVehicleHP = msg.ReadByte();
            }
            if (GameServerManager.responderGame != null)
            {
                GameServerManager.responderGame.GameServerSentVehicleHitPlayer(fromPlayerID2, toPlayerID2, fromVehicleIndex, num33, optionalTargetVehicleHP, didKillPlayer);
                return;
            }*/
        }

        //send 63
        private void serverSendPlayerHamsterballBounce(NetIncomingMessage message)
        {
            Player plr = playerList[getPlayerArrayIndex(message.SenderConnection)];
            NetOutgoingMessage smsg = server.CreateMessage();
            smsg.Write((byte)63);
            smsg.Write(plr.assignedID);
            smsg.Write(plr.vehicleID);
            server.SendToAll(smsg, NetDeliveryMethod.ReliableUnordered);
            Logger.DebugServer("I am done with my method -- 63");
        }
        //send 75
        private void serverSendAttackWindUp(NetIncomingMessage message)
        {
            NetOutgoingMessage msg = server.CreateMessage();
            msg.Write((byte)75);
            msg.Write(getPlayerID(message.SenderConnection)); //playerID
            msg.Write(message.ReadInt16()); //weaponID
            msg.Write(message.ReadByte()); //slotIndex
            server.SendToAll(msg, NetDeliveryMethod.ReliableUnordered);
        }

        //send 77
        private void serverSendAttackWindDown(short plrID, short weaponID)
        {
            NetOutgoingMessage msg = server.CreateMessage();
            msg.Write((byte)77);
            msg.Write(plrID);
            msg.Write(weaponID);
            server.SendToAll(msg, NetDeliveryMethod.ReliableUnordered);
        }
        //Helper Function to get playerID
        private short getPlayerID(NetConnection thisSender)
        {
            short id = -1;
            for (byte i = 0; i < playerList.Length; i++)
            {
                if (playerList[i] != null)
                {
                    if (playerList[i].sender == thisSender)
                    {
                        id = playerList[i].assignedID;
                        break;
                    }
                }
            }
            return id;
        }//Helper Function to get playerID
        private short getPlayerArrayIndex(NetConnection thisSender)
        {
            short id = -1;
            for (id = 0;  id < playerList.Length; id++)
            {
                if (playerList[id] != null)
                {
                    if (playerList[id].sender == thisSender)
                    {
                        Logger.Header($"Theoretical returned ID should be: {id}");
                        break;
                    }
                }
            }
            Logger.Header($"Returned ID will be: {id}");
            return id;
        }
    }

}
