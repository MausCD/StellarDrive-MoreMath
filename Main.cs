using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Core.Services;
using FishNet.Connection;
using HarmonyLib;
using Managers.Interface;
using Managers.Interface.Model;
using Managers.Interface.Services;
using Managers.Server.Network.Serialization;
using Managers.Server.Services;
using MelonLoader;
using MelonLoader.TinyJSON;
using Players;
using Players.Interface.Model;
using Players.Services;
using Saving.Services.Converters;
using Ships;
using Ships.Cables;
using Ships.Cables.Model;
using Ships.Interface.Model.Parts;
using Ships.Interface.Model.Parts.Common;
using Ships.Interface.Model.Parts.State;
using Ships.Interface.Model.Parts.StateTypes;
using Ships.Interface.Settings;
using Ships.Parts.Common;
using Ships.Parts.MathGate;
using Ships.Parts.UnaryMathGate;
using Ships.Services;
using TMPro;
using UI.BuildMenu;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Windows;
using static UnityEngine.Random;
using OperationType =
    Ships.Cables.SignalConnectionsServerUpdater.OperationType;
using SignalEvaluationOperation =
    Ships.Cables.SignalConnectionsServerUpdater.SignalEvaluationOperation;

namespace MoreMath
{
    public static class BuildInfo
    {
        public const string Name = "MoreMath";
        public const string Description = "Adds More Math Operations";
        public const string Author = "MausCD";
        public const string Company = null;
        public const string Version = "1.1.0";
        public const string DownloadLink = "https://github.com/MausCD/StellarDrive-MoreMath/releases";
    }

    public class MoreMath : MelonMod
    {
        public static MelonPreferences_Category Category;
        public static MelonPreferences_Entry<bool> ReplaceTexture;
        public static MelonPreferences_Entry<bool> SaveStatus;

        public override void OnInitializeMelon()
        {
            MoreMath.Category = MelonPreferences.CreateCategory("MoreMath");
            MoreMath.ReplaceTexture = MoreMath.Category.CreateEntry<bool>("ReplaceTexture", true, null, "Replaces the Signal Processor and Signal Combinator Textures to Display (I)nput and (Q)utput", false, false, null, null);
            MoreMath.SaveStatus = MoreMath.Category.CreateEntry<bool>("SaveStatus", true, null, "Saves the Signal Status of the RS , SR and Toggle Function", false, false, null, null);
        }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            if (buildIndex != 1) return;

            MelonLogger.Msg("Thanks for Using my Mod ^^ More on https://stellardrive.mauscd.de");
        }

        public override void OnSceneWasUnloaded(int buildIndex, string sceneName)
        {
            if (buildIndex != 1) return;
        }

        public override void OnUpdate()
        {
            if (MoreMath.ReplaceTexture.Value)
            {
                LogicGatesTextureReplacement.Update();
            }
        }
    }

    public enum CustomMathOperation : byte
    {
        Power = 10,
        Round = 11,
        LesserSameThan = 12,
        GreaterSameThan = 13,
        Rest = 14,
        Clamp = 15,
        RS = 16,
        SR = 17,
        OnDelay = 18,
        OffDelay = 19,
        Log = 20,
    }

    public enum CustomSignalOperation : byte
    {
        Floor = 10,
        Toggle = 11,
    }


    [HarmonyPatch(typeof(MathGateInteraction), "Interact")]
    public static class MathGateInteractionPatch
    {
        private static bool Prefix(MathGateInteraction __instance, InteractionType type, PartContext ___PartContext, IShipInteractionNetworkService ___InteractionService, ref bool __result)
        {
            if (type != InteractionType.Interact)
            {
                return true;
            }
            if (!ServiceLocator.GetService<ShipsClientTracker>().TryGetPartState<MathGateState>(___PartContext, out var partState))
            {
                __result = false;
                return false;
            }

            byte b = (byte)(partState.Operation + 1);

            bool isVanilla = Enum.IsDefined(typeof(MathGateOperation), b);
            bool isCustom = Enum.IsDefined(typeof(CustomMathOperation), b);

            if (!isVanilla && !isCustom)
            {
                b = 0;
            }

            ___InteractionService.ChangeMathOperation(new MathOperationNetworkRequest
            {
                ShipId = ___PartContext.ShipId,
                PartId = ___PartContext.PartId,
                OperationId = b
            });

            __result = true;
            return false;
        }
    }


    [HarmonyPatch(typeof(SignalProcessorInteraction), "Interact")]
    public static class SignalProcessorInteractionPatch
    {
        private static bool Prefix(SignalProcessorInteraction __instance, InteractionType type, PartContext ___PartContext, IShipInteractionNetworkService ___InteractionService, ref bool __result)
        {
            if (type != InteractionType.Interact)
            {
                return true;
            }
            if (!ServiceLocator.GetService<ShipsClientTracker>().TryGetPartState<SignalProcessorState>(___PartContext, out var partState))
            {
                __result = false;
                return false;
            }

            byte b = (byte)(partState.Operation + 1);

            bool isVanilla = Enum.IsDefined(typeof(SignalProcessorOperation), b);
            bool isCustom = Enum.IsDefined(typeof(CustomSignalOperation), b);

            if (!isVanilla && !isCustom)
            {
                b = 0;
            }

            ___InteractionService.ChangeMathOperation(new MathOperationNetworkRequest
            {
                ShipId = ___PartContext.ShipId,
                PartId = ___PartContext.PartId,
                OperationId = b
            });
            __result = true;
            return false;
        }
    }


    [HarmonyPatch(typeof(ShipInteractionService), "RpcLogic___ChangeMathOperation_ServerRPC___2879021062")]
    public static class ShipInteractionServicePatch
    {
        private static bool Prefix(ShipInteractionService __instance, MathOperationNetworkRequest __0, NetworkConnection __1)
        {
            var request = __0;
            var connection = __1;

            if (connection == null)
                return false;

            var ships = Traverse.Create(__instance)
                .Field("_ships")
                .GetValue<ShipsServerTracker>();

            TrackedShipServer ship = ships.GetShip(request.ShipId);

            if (ship == null ||
                !ship.TryGetStatefulPart(request.PartId, out var part))
            {
                return false;
            }

            if (part.State is MathGateState)
            {
                bool isVanilla = Enum.IsDefined(
                    typeof(MathGateOperation), request.OperationId);

                if (isVanilla)
                    return true;

                bool isCustom = Enum.IsDefined(
                    typeof(CustomMathOperation), request.OperationId);

                if (!isCustom)
                    return false;
            }
            else if (part.State is SignalProcessorState)
            {
                bool isVanilla = Enum.IsDefined(
                    typeof(SignalProcessorOperation), request.OperationId);

                if (isVanilla)
                    return true;

                bool isCustom = Enum.IsDefined(
                    typeof(CustomSignalOperation), request.OperationId);

                if (!isCustom)
                    return false;
            }
            else
            {
                return true;
            }

            var players = Traverse.Create(__instance)
                .Field("_players")
                .GetValue<PlayersServerTracker>();

            var player = players.GetTrackedPlayerFromConnectionId(
                connection.ClientId);

            if (player == null ||
                player.StateVal != PlayerStateType.Walking)
            {
                return false;
            }

            if (part.State is MathGateState mathGateState)
            {
                MathGateState updated = mathGateState with
                {
                    Operation = (MathGateOperation)request.OperationId
                };

                ship.UpdatePartState(request.PartId, updated);
            }
            else if (part.State is SignalProcessorState processorState)
            {
                SignalProcessorState updated = processorState with
                {
                    Operation = (SignalProcessorOperation)request.OperationId
                };

                ship.UpdatePartState(request.PartId, updated);
            }

            return false;
        }
    }



    [HarmonyPatch(typeof(MathGateVisuals), "SetNetworkState")]
    public static class MathGateVisualsPatch
    {
        private static void Postfix(MathGateVisuals __instance, IStatefulPartState state, TMP_Text ___textField)
        {
            if (state is not MathGateState mathState)
            {
                return;
            }

            switch ((CustomMathOperation)mathState.Operation)
            {
                case CustomMathOperation.Power:
                    ___textField.text = "^";
                    break;
                case CustomMathOperation.Round:
                    ___textField.text = "Round";
                    break;
                case CustomMathOperation.LesserSameThan:
                    ___textField.text = "≤";
                    break;
                case CustomMathOperation.GreaterSameThan:
                    ___textField.text = "≥";
                    break;
                case CustomMathOperation.Rest:
                    ___textField.text = "%";
                    break;
                case CustomMathOperation.Clamp:
                    ___textField.text = "Clamp";
                    break;
                case CustomMathOperation.RS:
                    ___textField.text = "RS";
                    break;
                case CustomMathOperation.SR:
                    ___textField.text = "SR";
                    break;
                case CustomMathOperation.OnDelay:
                    ___textField.text = "TON";
                    break;
                case CustomMathOperation.OffDelay:
                    ___textField.text = "TOF";
                    break;
                case CustomMathOperation.Log:
                    ___textField.text = "Log";
                    break;
            }
        }
    }


    [HarmonyPatch(typeof(SignalProcessorVisuals), "SetNetworkState")]
    public static class SignalProcessorVisualsPatch
    {
        private static void Postfix(
            SignalProcessorVisuals __instance,
            IStatefulPartState state)
        {
            if (state is not SignalProcessorState processorState)
                return;

            string text;

            switch ((CustomSignalOperation)processorState.Operation)
            {
                case CustomSignalOperation.Floor:
                    text = "Floor";
                    break;
                case CustomSignalOperation.Toggle:
                    text = "Toggle";
                    break;

                default:
                    return;
            }

            Traverse.Create(__instance)
                .Field("textField")
                .Property("text")
                .SetValue(text);
        }
    }

    [HarmonyPatch(typeof(SignalConnectionsServerUpdater), "ExecuteOperations")]
    public static class SignalConnectionsServerUpdaterPatch
    {
        private static bool Prefix(SignalConnectionsServerUpdater __instance, float deltaTime, List<SignalEvaluationOperation> ____operations, ShipsServerTracker ____ships, ConnectionsServerTracker ____connections)
        {
            var _operations = ____operations;
            var _ships = ____ships;
            var _connections = ____connections;
            foreach (SignalEvaluationOperation operation in _operations)
            {
                if (operation.Operation == OperationType.EvaluatePart)
                {
                    TrackedShipServer ship = _ships.GetShip(operation.Part.ShipId);
                    if (ship == null || !ship.TryGetStatefulPart(operation.Part.PartId, out var part))
                    {
                        continue;
                    }
                    IStatefulPartState state = part.State;
                    if (!(state is MathGateState mathGateState))
                    {
                        if (!(state is DeltaState deltaState))
                        {
                            if (!(state is SignalTransmitterState signalTransmitterState))
                            {
                                if (state is SignalProcessorState signalProcessorState)
                                {
                                    double socketSignalValue = _connections.GetSocketSignalValue(new EntityPartSocket
                                    {
                                        EntityId = ship.Id,
                                        Socket = new PartSocket(part.Id, 0)
                                    });
                                    double output = signalProcessorState.Operation switch
                                    {
                                        SignalProcessorOperation.SquareRoot => (socketSignalValue < 0.0) ? 0.0 : Math.Sqrt(socketSignalValue),
                                        SignalProcessorOperation.Abs => Math.Abs(socketSignalValue),
                                        SignalProcessorOperation.Sin => Math.Sin(socketSignalValue),
                                        SignalProcessorOperation.Cos => Math.Cos(socketSignalValue),
                                        SignalProcessorOperation.Tan => Math.Tan(socketSignalValue),
                                        SignalProcessorOperation.Asin => Math.Asin(Math.Clamp(socketSignalValue, -1.0, 1.0)),
                                        SignalProcessorOperation.Acos => Math.Acos(Math.Clamp(socketSignalValue, -1.0, 1.0)),
                                        SignalProcessorOperation.Atan => Math.Atan(socketSignalValue),
                                        SignalProcessorOperation.Negation => socketSignalValue * -1.0,
                                        SignalProcessorOperation.Delta => (deltaTime == 0f) ? 0.0 : ((socketSignalValue - signalProcessorState.PreviousValue) / (double)deltaTime),
                                        (SignalProcessorOperation)CustomSignalOperation.Floor => Math.Floor(socketSignalValue),
                                        (SignalProcessorOperation)CustomSignalOperation.Toggle => socketSignalValue > 0.0 && signalProcessorState.PreviousValue <= 0.0 ? (signalProcessorState.Output > 0.0 ? 0.0 : 1.0) : (signalProcessorState.Output > 0.0 ? 1.0 : 0.0),
                                        _ => 0.0,
                                    };
                                    SignalProcessorState state2 = signalProcessorState with
                                    {
                                        PreviousValue = socketSignalValue,
                                        Output = output
                                    };
                                    ship.UpdatePartState(part.Id, state2);
                                }
                            }
                            else
                            {
                                double socketSignalValue2 = _connections.GetSocketSignalValue(new EntityPartSocket
                                {
                                    EntityId = ship.Id,
                                    Socket = new PartSocket(part.Id, 0)
                                });
                                SignalTransmitterState state3 = signalTransmitterState with
                                {
                                    SignalValue = socketSignalValue2
                                };
                                ship.UpdatePartState(part.Id, state3);
                            }
                        }
                        else
                        {
                            double socketSignalValue3 = _connections.GetSocketSignalValue(new EntityPartSocket
                            {
                                EntityId = ship.Id,
                                Socket = new PartSocket(part.Id, 0)
                            });
                            double deltaValue = socketSignalValue3 - deltaState.PreviousValue;
                            DeltaState state4 = deltaState with
                            {
                                PreviousValue = socketSignalValue3,
                                DeltaValue = deltaValue
                            };
                            ship.UpdatePartState(part.Id, state4);
                        }
                    }
                    else
                    {
                        double socketSignalValue4 = _connections.GetSocketSignalValue(new EntityPartSocket
                        {
                            EntityId = ship.Id,
                            Socket = new PartSocket(part.Id, 0)
                        });
                        double socketSignalValue5 = _connections.GetSocketSignalValue(new EntityPartSocket
                        {
                            EntityId = ship.Id,
                            Socket = new PartSocket(part.Id, 1)
                        });
                        if (mathGateState.Operation != (MathGateOperation)CustomMathOperation.OnDelay && mathGateState.Operation != (MathGateOperation)CustomMathOperation.OffDelay) 
                        {
                            CombinatorTimers.Reset(part);
                        }
                        double output2 = mathGateState.Operation switch
                        {
                            MathGateOperation.Multiply => socketSignalValue4 * socketSignalValue5,
                            MathGateOperation.Add => socketSignalValue4 + socketSignalValue5,
                            MathGateOperation.Subtract => socketSignalValue4 - socketSignalValue5,
                            MathGateOperation.Divide => (socketSignalValue5 == 0.0) ? 0.0 : (socketSignalValue4 / socketSignalValue5),
                            MathGateOperation.Min => Math.Min(socketSignalValue4, socketSignalValue5),
                            MathGateOperation.Max => Math.Max(socketSignalValue4, socketSignalValue5),
                            MathGateOperation.Atan2 => Math.Atan2(socketSignalValue4, socketSignalValue5),
                            MathGateOperation.LesserThan => (socketSignalValue4 < socketSignalValue5) ? 1 : 0,
                            MathGateOperation.GreaterThan => (socketSignalValue4 > socketSignalValue5) ? 1 : 0,
                            MathGateOperation.Equals => (socketSignalValue4 == socketSignalValue5) ? 1 : 0,
                            (MathGateOperation)CustomMathOperation.Power => Math.Pow(socketSignalValue4, socketSignalValue5),
                            (MathGateOperation)CustomMathOperation.Round => Math.Round((Math.Pow(10, socketSignalValue5) * socketSignalValue4)) / Math.Pow(10, socketSignalValue5),
                            (MathGateOperation)CustomMathOperation.LesserSameThan => ((socketSignalValue4 < socketSignalValue5) || socketSignalValue4 == socketSignalValue5) ? 1 : 0,
                            (MathGateOperation)CustomMathOperation.GreaterSameThan => ((socketSignalValue4 > socketSignalValue5) || socketSignalValue4 == socketSignalValue5) ? 1 : 0,
                            (MathGateOperation)CustomMathOperation.Clamp => Math.Clamp(socketSignalValue4, -Math.Abs(socketSignalValue5), Math.Abs(socketSignalValue5)),
                            (MathGateOperation)CustomMathOperation.Rest => socketSignalValue5 == 0.0 ? 0.0 : socketSignalValue4 % socketSignalValue5,
                            (MathGateOperation)CustomMathOperation.RS => socketSignalValue5 > 0.0 ? 0.0 : socketSignalValue4 > 0.0 ? 1.0 : (mathGateState.Output > 0.0 ? 1.0 : 0.0),
                            (MathGateOperation)CustomMathOperation.SR => socketSignalValue4 > 0.0 ? 1.0 : socketSignalValue5 > 0.0 ? 0.0 : (mathGateState.Output > 0.0 ? 1.0 : 0.0),
                            (MathGateOperation)CustomMathOperation.OnDelay => CombinatorTimers.Evaluate(part, CustomMathOperation.OnDelay, socketSignalValue4, socketSignalValue5, deltaTime),
                            (MathGateOperation)CustomMathOperation.OffDelay => CombinatorTimers.Evaluate(part, CustomMathOperation.OffDelay, socketSignalValue4, socketSignalValue5, deltaTime),
                            (MathGateOperation)CustomMathOperation.Log => socketSignalValue4 > 0.0 && !double.IsInfinity(socketSignalValue4) && socketSignalValue5 > 0.0 && socketSignalValue5 != 1.0 && !double.IsInfinity(socketSignalValue5) ? Math.Log(socketSignalValue4, socketSignalValue5) : 0.0,
                            _ => 0.0,
                        };
                        MathGateState state5 = mathGateState with
                        {
                            Output = output2
                        };
                        ship.UpdatePartState(part.Id, state5);
                    }
                }
                else if (operation.Operation == OperationType.EvaluateCableGroup)
                {
                    _connections.UpdateSignalGroupValue(operation.CableGroupId);
                }
            }
            return false;
        }
    }

    [HarmonyPatch(typeof(BuildMenuPartInfo), "Show")]
    public static class BuildMenuPartInfoPatch
    {
        private static void Prefix(PartSettings __0)
        {
            var settings = __0;

            if (settings == null)
                return;

            if (settings.localizedDescription != null &&
                !settings.localizedDescription.IsEmpty)
            {
                return;
            }

            string stateClass = settings.internalStateClass?
                .Split(',')[0]
                .Trim() ?? "";

            string addition;

            if (stateClass == typeof(MathGateState).FullName ||
                settings.fullLabel == "Signal Combinator")
            {
                addition =
    "\n\nOperations:\n" +
    "Multiply (×), Add (+), Subtract (−), Divide (÷), " +
    "Min, Max, Atan2, Less than (<), Greater than (>), Equal (=).\n" +
    "\nMoreMath:\n" +
    "Power (^), Round, Less or equal (≤), Greater or equal (≥), " +
    "Remainder (%), Clamp , RS , SR , TON , TOF , Log";
            }
            else if (stateClass == typeof(SignalProcessorState).FullName ||
                     settings.fullLabel == "Signal Processor")
            {
                addition =
                    "\n\nMoreMath operations:\n" +
                    "Floor , Toggle";
            }
            else
            {
                return;
            }

            string description = settings.description ?? "";

            if (!description.Contains(addition))
                settings.description = description + addition;
        }
    }

    internal static class PersistentSignalEncoding
    {
        private const byte OperationMask = 0x3F;
        private const byte PreviousInputBit = 0x40;
        private const byte OutputBit = 0x80;

        public static bool IsLatch(byte operation)
        {
            return operation == (byte)CustomMathOperation.RS ||
                   operation == (byte)CustomMathOperation.SR;
        }

        public static bool IsToggle(byte operation)
        {
            return operation == (byte)CustomSignalOperation.Toggle;
        }

        public static byte Encode(
            byte operation,
            double output,
            bool previousInputActive = false)
        {
            byte encoded = operation;

            if (output > 0.0)
                encoded |= OutputBit;

            if (previousInputActive)
                encoded |= PreviousInputBit;

            return encoded;
        }

        public static byte DecodeOperation(byte encoded)
        {
            return (byte)(encoded & OperationMask);
        }

        public static double DecodeOutput(byte encoded)
        {
            return (encoded & OutputBit) != 0 ? 1.0 : 0.0;
        }

        public static double DecodePreviousInput(byte encoded)
        {
            return (encoded & PreviousInputBit) != 0 ? 1.0 : 0.0;
        }
    }

    [HarmonyPatch(typeof(ShipsConverter),"ConvertShipStateToSerializable")]
    public static class SavePersistentSignalsPatch
    {
        private static void Postfix(
            Ships.Interface.Model.State.IShipState __0,
            ref Saving.Serialization.V15.Ships.SerializableShip __result)
        {
            if (!MoreMath.SaveStatus.Value) return;
            var partsById = __0.StatefulParts
                .ToDictionary(part => part.Id);

            if (__result.mathGateStates != null)
            {
                for (int i = 0; i < __result.mathGateStates.Length; i++)
                {
                    var saved = __result.mathGateStates[i];

                    if (!PersistentSignalEncoding.IsLatch(saved.operationId))
                        continue;

                    if (!partsById.TryGetValue(saved.partId, out var part) ||
                        part.State is not MathGateState state)
                    {
                        continue;
                    }

                    saved.operationId = PersistentSignalEncoding.Encode(
                        saved.operationId,
                        state.Output);

                    __result.mathGateStates[i] = saved;
                }
            }

            if (__result.unaryMathGateStates != null)
            {
                for (int i = 0; i < __result.unaryMathGateStates.Length; i++)
                {
                    var saved = __result.unaryMathGateStates[i];

                    if (!PersistentSignalEncoding.IsToggle(saved.operationId))
                        continue;

                    if (!partsById.TryGetValue(saved.partId, out var part) ||
                        part.State is not SignalProcessorState state)
                    {
                        continue;
                    }

                    saved.operationId = PersistentSignalEncoding.Encode(
                        saved.operationId,
                        state.Output,
                        state.PreviousValue > 0.0);

                    __result.unaryMathGateStates[i] = saved;
                }
            }
        }
    }

    [HarmonyPatch(typeof(ShipPartConverter),"ConvertToFloatingPartState")]
    public static class LoadPersistentSignalsPatch
    {
        private static void Postfix(StatefulPart[] __result)
        {
            if (!MoreMath.SaveStatus.Value) return;

            if (__result == null)
                return;

            foreach (var part in __result)
            {
                if (part.State is MathGateState mathState)
                {
                    byte encoded = (byte)mathState.Operation;

                    byte operation =
                        PersistentSignalEncoding.DecodeOperation(encoded);

                    if (!PersistentSignalEncoding.IsLatch(operation))
                        continue;

                    part.State = mathState with
                    {
                        Operation = (MathGateOperation)operation,
                        Output =
                            PersistentSignalEncoding.DecodeOutput(encoded)
                    };
                }
                else if (part.State is SignalProcessorState processorState)
                {
                    byte encoded = (byte)processorState.Operation;

                    byte operation =
                        PersistentSignalEncoding.DecodeOperation(encoded);

                    if (!PersistentSignalEncoding.IsToggle(operation))
                        continue;

                    part.State = processorState with
                    {
                        Operation = (SignalProcessorOperation)operation,
                        Output =
                            PersistentSignalEncoding.DecodeOutput(encoded),
                        PreviousValue =
                            PersistentSignalEncoding.DecodePreviousInput(encoded)
                    };
                }
            }
        }
    }

    internal static class LogicGatesTextureReplacement
    {
        private static Texture2D? replacement;
        private static bool loadAttempted;
        private static float nextCheck;

        public static void Update()
        {
            // Nur alle 5 Sekunden prüfen, nicht in jedem Frame.
            if (Time.realtimeSinceStartup < nextCheck)
                return;

            nextCheck = Time.realtimeSinceStartup + 5f;

            if (!loadAttempted)
            {
                loadAttempted = true;
                LoadTexture();
            }

            if (replacement == null)
                return;

            foreach (Material material in
                     Resources.FindObjectsOfTypeAll<Material>())
            {
                if (material == null)
                    continue;

                if (material.name != "LogicGates" &&
                    material.name != "LogicGates (Instance)")
                {
                    continue;
                }

                if (!material.HasProperty("_Albedo"))
                    continue;

                // Bereits ersetzte Texturen nicht erneut setzen.
                if (material.GetTexture("_Albedo") == replacement)
                    continue;

                material.SetTexture("_Albedo", replacement);
            }
        }

        private static void LoadTexture()
        {
            try
            {
                var assembly =
                    typeof(LogicGatesTextureReplacement).Assembly;

                string resourceName = assembly
                    .GetManifestResourceNames()
                    .FirstOrDefault(name =>
                        name == "8304ebbf.png" ||
                        name.EndsWith(
                            ".8304ebbf.png",
                            StringComparison.OrdinalIgnoreCase));

                if (resourceName == null)
                {
                    MelonLogger.Error(
                        "8304ebbf.png not found. " +
                        "Set its Build Action to Embedded Resource.");
                    return;
                }

                using var stream =
                    assembly.GetManifestResourceStream(resourceName);

                if (stream == null)
                {
                    MelonLogger.Error("Could not open texture resource.");
                    return;
                }

                using var buffer = new System.IO.MemoryStream();
                stream.CopyTo(buffer);

                var texture = new Texture2D(2, 2);

                if (!ImageConversion.LoadImage(texture, buffer.ToArray()))
                {
                    UnityEngine.Object.Destroy(texture);
                    MelonLogger.Error("Could not decode 8304ebbf.png.");
                    return;
                }

                texture.name = "MoreMath_LogicGates_Replacement";
                UnityEngine.Object.DontDestroyOnLoad(texture);

                replacement = texture;
                MelonLogger.Msg("Embedded texture loaded.");
            }
            catch (Exception exception)
            {
                MelonLogger.Error(
                    $"Texture loading failed: {exception}");
            }
        }
    }

    internal static class CombinatorTimers
    {
        private sealed class TimerState
        {
            public byte Operation;
            public double Elapsed;
            public bool Output;
            public bool PreviousInput;
        }

        private static readonly
            System.Runtime.CompilerServices.ConditionalWeakTable<
                StatefulPart, TimerState> States = new();

        public static void Reset(StatefulPart part)
        {
            States.Remove(part);
        }

        public static double Evaluate(
            StatefulPart part,
            CustomMathOperation operation,
            double a,
            double b,
            float deltaTime)
        {
            byte operationId = (byte)operation;

            if (!States.TryGetValue(part, out var timer) ||
                timer.Operation != operationId)
            {
                States.Remove(part);

                timer = new TimerState
                {
                    Operation = operationId
                };

                States.Add(part, timer);
            }

            bool input = a > 0.0;

            double delay =
                double.IsNaN(b) || b < 0.0 ? 0.0 : b;

            double dt =
                float.IsNaN(deltaTime) ||
                float.IsInfinity(deltaTime) ||
                deltaTime < 0f
                    ? 0.0
                    : deltaTime;

            switch (operation)
            {
                case CustomMathOperation.OnDelay:
                    if (!input)
                    {
                        timer.Elapsed = 0.0;
                        timer.Output = false;
                    }
                    else
                    {
                        // Beim ersten erkannten Einschalten
                        // beginnt die Zeit bei null.
                        timer.Elapsed = timer.PreviousInput
                            ? timer.Elapsed + dt
                            : 0.0;

                        timer.Output = timer.Elapsed >= delay;
                    }
                    break;

                case CustomMathOperation.OffDelay:
                    if (input)
                    {
                        timer.Elapsed = 0.0;
                        timer.Output = true;
                    }
                    else if (timer.Output)
                    {
                        // Beim ersten erkannten Abschalten
                        // beginnt der Nachlauf bei null.
                        timer.Elapsed = timer.PreviousInput
                            ? 0.0
                            : timer.Elapsed + dt;

                        if (timer.Elapsed >= delay)
                        {
                            timer.Output = false;
                            timer.Elapsed = 0.0;
                        }
                    }
                    else
                    {
                        timer.Elapsed = 0.0;
                    }
                    break;
            }

            timer.PreviousInput = input;

            return timer.Output ? 1.0 : 0.0;
        }
    }
}