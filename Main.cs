using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Xml;
using Core.Services;
using Core.Values;
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
using UI.Common;
using UI.Interface;
using UI.Interface.Model;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
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
        public const string Version = "1.2.0";
        public const string DownloadLink = "https://github.com/MausCD/StellarDrive-MoreMath/releases";
    }

    public class MoreMath : MelonMod
    {
        public static MelonPreferences_Category Category;
        public static MelonPreferences_Entry<bool> ReplaceTexture;
        public static MelonPreferences_Entry<bool> SaveStatus;
        public static MelonPreferences_Entry<bool> EnableMenu;

        public override void OnInitializeMelon()
        {
            MoreMath.Category = MelonPreferences.CreateCategory("MoreMath");
            MoreMath.ReplaceTexture = MoreMath.Category.CreateEntry<bool>("ReplaceTexture", true, null, "Replaces the Signal Processor and Signal Combinator Textures to Display (I)nput and (Q)utput", false, false, null, null);
            MoreMath.SaveStatus = MoreMath.Category.CreateEntry<bool>("SaveStatus", true, null, "Saves the Signal Status of the RS , SR and Toggle Function", false, false, null, null);
            MoreMath.EnableMenu = MoreMath.Category.CreateEntry<bool>("EnableMenu", true, null, "Enables the Selector Menu instead of switching thru all Operators", false, false, null, null);
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
        Or = 21,
        Xor = 22,
        Nand = 23,
        Nor = 24,
        NotEuqal = 25,
        Hypot = 26,
        Deadband = 27,
        SchmittTrigger = 28,
    }

    public enum CustomSignalOperation : byte
    {
        Floor = 10,
        Toggle = 11,
        OneWay = 12,
        Ceil = 13,
        Not = 14,
        Sign = 15,
        Integrator = 16,
        Exp = 17,
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

            if (MoreMath.EnableMenu.Value)
            {
                NativeOperatorSelectorUI.Open(true, ___PartContext, ___InteractionService);
                __result = true;
                return false;
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

            if (MoreMath.EnableMenu.Value)
            {
                NativeOperatorSelectorUI.Open(false, ___PartContext, ___InteractionService);
                __result = true;
                return false;
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
                case CustomMathOperation.Or:
                    ___textField.text = "OR";
                    break;
                case CustomMathOperation.Xor:
                    ___textField.text = "XOR";
                    break;
                case CustomMathOperation.Nand:
                    ___textField.text = "NAND";
                    break;
                case CustomMathOperation.Nor:
                    ___textField.text = "NOR";
                    break;
                case CustomMathOperation.NotEuqal:
                    ___textField.text = "≠";
                    break;
                case CustomMathOperation.Hypot:
                    ___textField.text = "Hypot";
                    break;
                case CustomMathOperation.Deadband:
                    ___textField.text = "Deadband";
                    break;
                case CustomMathOperation.SchmittTrigger:
                    ___textField.text = "Schmitt";
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
                case CustomSignalOperation.OneWay:
                    text = "->|";
                    break;
                case CustomSignalOperation.Ceil:
                    text = "Ceil";
                    break;
                case CustomSignalOperation.Not:
                    text = "!";
                    break;
                case CustomSignalOperation.Sign:
                    text = "sgn";
                    break;
                case CustomSignalOperation.Integrator:
                    text = "∫";
                    break;
                case CustomSignalOperation.Exp:
                    text = "eˣ";
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
                                        (SignalProcessorOperation)CustomSignalOperation.OneWay => socketSignalValue,
                                        (SignalProcessorOperation)CustomSignalOperation.Ceil => Math.Ceiling(socketSignalValue),
                                        (SignalProcessorOperation)CustomSignalOperation.Not => socketSignalValue > 0.0 ? 0.0 : 1.0,
                                        (SignalProcessorOperation)CustomSignalOperation.Sign => Math.Sign(socketSignalValue),
                                        (SignalProcessorOperation)CustomSignalOperation.Integrator => signalProcessorState.Output + (socketSignalValue * (double)deltaTime),
                                        (SignalProcessorOperation)CustomSignalOperation.Exp => Math.Exp(socketSignalValue),
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
                            (MathGateOperation)CustomMathOperation.Or => (socketSignalValue4 > 0.0 || socketSignalValue5 > 0.0) ? 1.0 : 0.0,
                            (MathGateOperation)CustomMathOperation.Xor => ((socketSignalValue4 > 0.0) ^ (socketSignalValue5 > 0.0)) ? 1.0 : 0.0,
                            (MathGateOperation)CustomMathOperation.Nand => (socketSignalValue4 > 0.0 && socketSignalValue5 > 0.0) ? 0.0 : 1.0,
                            (MathGateOperation)CustomMathOperation.Nor => (socketSignalValue4 <= 0.0 && socketSignalValue5 <= 0.0) ? 1.0 : 0.0,
                            (MathGateOperation)CustomMathOperation.NotEuqal => socketSignalValue4 != socketSignalValue5 ? 1.0 : 0.0,
                            (MathGateOperation)CustomMathOperation.Hypot => Math.Sqrt((socketSignalValue4 * socketSignalValue4) + (socketSignalValue5 * socketSignalValue5)),
                            (MathGateOperation)CustomMathOperation.Deadband => Math.Abs(socketSignalValue4) < Math.Abs(socketSignalValue5) ? 0.0 : socketSignalValue4,
                            (MathGateOperation)CustomMathOperation.SchmittTrigger => socketSignalValue4 > socketSignalValue5 ? 1.0 : (socketSignalValue4 < -Math.Abs(socketSignalValue5) ? 0.0 : (mathGateState.Output > 0.0 ? 1.0 : 0.0)),
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

    public class NativeOperatorSelectorUI : MonoBehaviour, IClosableMenu
    {
        private const int PreferredColumns = 6;
        private const float WindowScale = 1f;
        private const bool DebugHierarchy = false;
        private const float CellScale = 1.2f;
        private const float FrameMargin = 24f;
        private const float TitleGap = -800f;

        private static NativeOperatorSelectorUI _instance;
        private GameObject _window;
        private bool _isOpen;
        private MenuTracker _menuTracker;
        private Core.Events.BoolGameEvent _buildMenuToggled;

        private bool _isMathGate;
        private PartContext _currentPartContext;
        private IShipInteractionNetworkService _interactionService;
        private List<InputActionMap> _disabledActionMaps = new List<InputActionMap>();
        private int _openFrame = -1;

        private Vector2 _cellSize;
        private TMP_FontAsset _gameFont;

        public struct OpInfo
        {
            public byte Id;
            public string Name;
            public string Symbol;

            public OpInfo(byte id, string name, string symbol)
            {
                Id = id;
                Name = name;
                Symbol = symbol;
            }
        }

        public static readonly OpInfo[] MathGateOps = new OpInfo[]
        {
            new OpInfo(0, "Multiply", "*"),
            new OpInfo(1, "Add", "+"),
            new OpInfo(2, "Subtract", "-"),
            new OpInfo(3, "Divide", "÷"),
            new OpInfo(4, "Min", "Min"),
            new OpInfo(5, "Max", "Max"),
            new OpInfo(6, "Atan2", "Atan2"),
            new OpInfo(7, "Less Than", "<"),
            new OpInfo(8, "Greater Than", ">"),
            new OpInfo(9, "Equal", "="),
            new OpInfo((byte)CustomMathOperation.Power, "Power", "^"),
            new OpInfo((byte)CustomMathOperation.Round, "Round", "Round"),
            new OpInfo((byte)CustomMathOperation.LesserSameThan, "Less Equal", "≤"),
            new OpInfo((byte)CustomMathOperation.GreaterSameThan, "Greater Equal", "≥"),
            new OpInfo((byte)CustomMathOperation.Rest, "Modulo", "%"),
            new OpInfo((byte)CustomMathOperation.Clamp, "Clamp", "Clamp"),
            new OpInfo((byte)CustomMathOperation.RS, "RS Latch", "RS"),
            new OpInfo((byte)CustomMathOperation.SR, "SR Latch", "SR"),
            new OpInfo((byte)CustomMathOperation.OnDelay, "On-Delay", "TON"),
            new OpInfo((byte)CustomMathOperation.OffDelay, "Off-Delay", "TOF"),
            new OpInfo((byte)CustomMathOperation.Log, "Logarithm", "Log"),
            new OpInfo((byte)CustomMathOperation.Or, "OR", "OR"),
            new OpInfo((byte)CustomMathOperation.Xor, "XOR", "XOR"),
            new OpInfo((byte)CustomMathOperation.Nand, "NAND", "NAND"),
            new OpInfo((byte)CustomMathOperation.Nor, "NOR", "NOR"),
            new OpInfo((byte)CustomMathOperation.NotEuqal, "Not Equal", "≠"),
            new OpInfo((byte)CustomMathOperation.Hypot, "Hypotenuse", "Hypot"),
            new OpInfo((byte)CustomMathOperation.Deadband, "Deadband", "Deadband"),
            new OpInfo((byte)CustomMathOperation.SchmittTrigger, "Schmitt Trigger", "Schmitt")
        };

        public static readonly OpInfo[] SignalProcessorOps = new OpInfo[]
        {
            new OpInfo(0, "Negation", "Neg"),
            new OpInfo(1, "Square Root", "Sqrt"),
            new OpInfo(2, "Absolute", "Abs"),
            new OpInfo(3, "Delta", "Δ"),
            new OpInfo(4, "Sine", "Sin"),
            new OpInfo(5, "Cosine", "Cos"),
            new OpInfo(6, "Tangent", "Tan"),
            new OpInfo(7, "Arc Sine", "Asin"),
            new OpInfo(8, "Arc Cosine", "Acos"),
            new OpInfo(9, "Arc Tangent", "Atan"),
            new OpInfo((byte)CustomSignalOperation.Floor, "Floor", "Floor"),
            new OpInfo((byte)CustomSignalOperation.Toggle, "Toggle", "Toggle"),
            new OpInfo((byte)CustomSignalOperation.OneWay, "OneWay", "->|"),
            new OpInfo((byte)CustomSignalOperation.Ceil, "Ceil", "Ceil"),
            new OpInfo((byte)CustomSignalOperation.Not, "NOT", "!"),
            new OpInfo((byte)CustomSignalOperation.Sign, "Sign", "sgn"),
            new OpInfo((byte)CustomSignalOperation.Integrator, "Integrator", "∫"),
            new OpInfo((byte)CustomSignalOperation.Exp, "Exponential", "eˣ")
        };

        public static void Open(bool isMathGate, PartContext context, IShipInteractionNetworkService service)
        {
            if (_instance == null)
            {
                GameObject host = new GameObject("MoreMath_NativeUI_Host");
                UnityEngine.Object.DontDestroyOnLoad(host);
                _instance = host.AddComponent<NativeOperatorSelectorUI>();
            }

            if (_instance._isOpen)
                _instance.Close();

            _instance._isMathGate = isMathGate;
            _instance._currentPartContext = context;
            _instance._interactionService = service;
            _instance.ShowMenu();
        }

        private void Update()
        {
            if (!_isOpen) return;
            if (Time.frameCount == _openFrame) return;

            var keyboard = Keyboard.current;
            if (keyboard != null &&
                (keyboard.escapeKey.wasPressedThisFrame || keyboard.fKey.wasPressedThisFrame))
            {
                Close();
            }
        }

        private void ShowMenu()
        {
            if (_window != null) return;

            GameObject staging = null;
            try
            {
                BuildMenuActivator activator = Resources.FindObjectsOfTypeAll<BuildMenuActivator>()
                    .FirstOrDefault(x => x != null && x.gameObject.scene.IsValid() && x.gameObject.scene.isLoaded);

                if (activator == null)
                {
                    return;
                }

                GameObject prefab = Traverse.Create(activator).Field("buildMenuPrefab").GetValue<GameObject>();
                Transform targetParent = activator.transform.parent;
                if (prefab == null || targetParent == null) return;

                _buildMenuToggled = Traverse.Create(activator).Field("buildMenuToggled")
                    .GetValue<Core.Events.BoolGameEvent>();

                staging = new GameObject("MoreMath_UI_Staging");
                staging.SetActive(false);

                _window = UnityEngine.Object.Instantiate(prefab, staging.transform, false);
                _window.name = "MoreMathOperatorMenu";
                _window.SetActive(false);

                BuildMenu menu = _window.GetComponentInChildren<BuildMenu>(true);
                if (menu == null)
                    throw new InvalidOperationException("Prefab enthält kein BuildMenu.");

                var fields = Traverse.Create(menu);
                Transform tabs = fields.Field("tabButtonsContainer").GetValue<Transform>();
                Transform content = fields.Field("tabPanelContainer").GetValue<Transform>();
                BuildPartButton buttonPrefab = fields.Field("buttonPrefab").GetValue<BuildPartButton>();
                BuildMenuPartInfo partInfo = fields.Field("partInfo").GetValue<BuildMenuPartInfo>();
                GameObject rowPrefab = fields.Field("rowPrefab").GetValue<GameObject>();
                GameObject tabPanelPrefab = fields.Field("tabPanelPrefab").GetValue<GameObject>();
                GameColorPalette palette = fields.Field("colorPalette").GetValue<GameColorPalette>();

                if (content == null || buttonPrefab == null)
                    throw new InvalidOperationException("tabPanelContainer oder buttonPrefab fehlt.");

                UnityEngine.Object.DestroyImmediate(menu);

                _window.transform.SetParent(targetParent, false);

                if (DebugHierarchy)
                    DumpHierarchy(_window.transform, 0);

                RectTransform panel = FindPanel(_window.transform, tabs, content);
                RectTransform contentRect = content as RectTransform;

                float headerHeight = 80f; 
                float padding = 20f;    

                if (contentRect != null)
                {
                    Rect c = GetRectIn(panel, contentRect);
                    float measuredHeader = panel.rect.yMax - c.yMax;
                    float measuredPad = c.xMin - panel.rect.xMin;

                    if (measuredHeader > 10f && measuredHeader < 400f) headerHeight = measuredHeader;
                    if (measuredPad > 4f && measuredPad < 150f) padding = measuredPad;
                }

                float titleBottom = 0f;
                RectTransform tabsMeasure = tabs as RectTransform;

                if (tabsMeasure != null)
                {
                    Rect t = GetRectIn(panel, tabsMeasure);
                    titleBottom = panel.rect.yMax - t.yMin;
                }

                RectTransform btnRect = buttonPrefab.transform as RectTransform;
                _cellSize = btnRect != null ? btnRect.rect.size : Vector2.zero;
                if (_cellSize.x < 10f || _cellSize.y < 10f)
                    _cellSize = new Vector2(120f, 120f);

                _cellSize *= CellScale;
                padding += FrameMargin;
                headerHeight = Mathf.Max(headerHeight, titleBottom + TitleGap);

                float spacingX = 8f;
                float spacingY = 8f;

                if (rowPrefab != null)
                {
                    var hlg = rowPrefab.GetComponent<HorizontalLayoutGroup>();
                    if (hlg != null) spacingX = Mathf.Max(hlg.spacing, 2f);
                }

                if (tabPanelPrefab != null)
                {
                    var vlg = tabPanelPrefab.GetComponent<VerticalLayoutGroup>();
                    if (vlg != null) spacingY = Mathf.Max(vlg.spacing, 2f);
                }

                TMP_Text prefabLabel = Traverse.Create(buttonPrefab).Field("text").GetValue<TMP_Text>();
                _gameFont = prefabLabel != null ? prefabLabel.font : null;

                RectTransform tabsRect = tabs as RectTransform;

                if (tabs != null) tabs.gameObject.SetActive(false);
                if (partInfo != null) partInfo.gameObject.SetActive(false);
                content.gameObject.SetActive(false);

                ScrollRect scroll = content.GetComponentInParent<ScrollRect>(true);
                if (scroll != null && scroll.transform != panel && scroll.transform.IsChildOf(panel))
                    scroll.gameObject.SetActive(false);

                HideInputControls(_window);

                OpInfo[] ops = _isMathGate ? MathGateOps : SignalProcessorOps;
                int count = ops.Length;

                RectTransform limitRect = targetParent as RectTransform;
                float maxW = float.MaxValue;
                float maxH = float.MaxValue;

                if (limitRect != null && limitRect.rect.width > 100f && limitRect.rect.height > 100f)
                {
                    maxW = limitRect.rect.width * 0.92f;
                    maxH = limitRect.rect.height * 0.88f;
                }

                int cols = Mathf.Min(PreferredColumns, count);
                float gridW, gridH, panelW, panelH;

                while (true)
                {
                    int rows = Mathf.CeilToInt(count / (float)cols);
                    gridW = cols * _cellSize.x + (cols - 1) * spacingX;
                    gridH = rows * _cellSize.y + (rows - 1) * spacingY;
                    panelW = gridW + padding * 2f;
                    panelH = headerHeight + gridH + padding;

                    if (panelH <= maxH || cols >= count || panelW + _cellSize.x > maxW)
                        break;

                    cols++;
                }

                ContentSizeFitter fitter = panel.GetComponent<ContentSizeFitter>();
                if (fitter != null) UnityEngine.Object.DestroyImmediate(fitter);

                panel.anchorMin = new Vector2(0.5f, 0.5f);
                panel.anchorMax = new Vector2(0.5f, 0.5f);
                panel.pivot = new Vector2(0.5f, 0.5f);
                panel.sizeDelta = new Vector2(panelW, panelH);
                panel.anchoredPosition = Vector2.zero;

                float fit = Mathf.Min(1f, maxW / panelW, maxH / panelH);
                if (float.IsNaN(fit) || fit < 0.3f)
                    fit = 1f;

                panel.localScale = Vector3.one * (fit * WindowScale);

                panel.localScale = Vector3.one * (fit * WindowScale);

                CreateTitle(tabsRect, panel, palette);

                GameObject gridObject = new GameObject("MoreMath_OpGrid", typeof(RectTransform), typeof(GridLayoutGroup), typeof(LayoutElement));
                gridObject.GetComponent<LayoutElement>().ignoreLayout = true;

                RectTransform gridRect = gridObject.GetComponent<RectTransform>();
                gridRect.SetParent(panel, false);
                gridRect.anchorMin = new Vector2(0f, 1f);
                gridRect.anchorMax = new Vector2(0f, 1f);
                gridRect.pivot = new Vector2(0f, 1f);
                gridRect.anchoredPosition = new Vector2(padding, -headerHeight);
                gridRect.sizeDelta = new Vector2(gridW, gridH);
                gridRect.SetAsLastSibling();

                GridLayoutGroup grid = gridObject.GetComponent<GridLayoutGroup>();
                grid.cellSize = _cellSize;
                grid.spacing = new Vector2(spacingX, spacingY);
                grid.padding = new RectOffset(0, 0, 0, 0);
                grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
                grid.startAxis = GridLayoutGroup.Axis.Horizontal;
                grid.childAlignment = TextAnchor.UpperLeft;
                grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                grid.constraintCount = cols;

                foreach (var op in ops)
                    CreateOperatorButton(buttonPrefab, gridRect, op);

                _window.transform.SetAsLastSibling();
                _window.SetActive(true);

                _menuTracker = ServiceLocator.GetService<MenuTracker>();

                DisableGameplayInputs();
                _isOpen = true;
                _openFrame = Time.frameCount;

                if (_menuTracker != null)
                    _menuTracker.TrackMenu(this);

                if (_buildMenuToggled != null)
                    _buildMenuToggled.Raise(true);
            }
            catch (Exception ex)
            {
                Close();
            }
            finally
            {
                if (staging != null)
                    UnityEngine.Object.Destroy(staging);
            }
        }

        private static RectTransform FindPanel(Transform root, Transform tabs, Transform content)
        {
            Transform result = null;

            if (tabs != null)
            {
                var chain = new HashSet<Transform>();
                for (Transform t = tabs; t != null; t = t.parent)
                    chain.Add(t);

                for (Transform t = content; t != null; t = t.parent)
                {
                    if (chain.Contains(t))
                    {
                        result = t;
                        break;
                    }
                }
            }

            if (result == null || result == root)
            {
                Transform t = content;
                while (t.parent != null && t.parent != root)
                    t = t.parent;
                result = t;
            }

            return (RectTransform)result;
        }

        private static Rect GetRectIn(RectTransform space, RectTransform target)
        {
            Vector3[] corners = new Vector3[4];
            target.GetWorldCorners(corners);

            float xMin = float.MaxValue, yMin = float.MaxValue;
            float xMax = float.MinValue, yMax = float.MinValue;

            for (int i = 0; i < 4; i++)
            {
                Vector3 p = space.InverseTransformPoint(corners[i]);
                xMin = Mathf.Min(xMin, p.x);
                yMin = Mathf.Min(yMin, p.y);
                xMax = Mathf.Max(xMax, p.x);
                yMax = Mathf.Max(yMax, p.y);
            }

            return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
        }

        private void CreateTitle(RectTransform tabsRect, RectTransform panel, GameColorPalette palette)
        {
            GameObject titleObj = new GameObject("MoreMath_MenuTitle", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
            titleObj.GetComponent<LayoutElement>().ignoreLayout = true;

            RectTransform titleRect = titleObj.GetComponent<RectTransform>();
            Transform parent = tabsRect != null && tabsRect.parent != null ? tabsRect.parent : panel;
            titleRect.SetParent(parent, false);

            if (tabsRect != null)
            {
                titleRect.anchorMin = tabsRect.anchorMin;
                titleRect.anchorMax = tabsRect.anchorMax;
                titleRect.pivot = tabsRect.pivot;
                titleRect.anchoredPosition = tabsRect.anchoredPosition;
                titleRect.sizeDelta = tabsRect.sizeDelta;
            }
            else
            {
                titleRect.anchorMin = new Vector2(0f, 1f);
                titleRect.anchorMax = new Vector2(1f, 1f);
                titleRect.pivot = new Vector2(0.5f, 1f);
                titleRect.anchoredPosition = new Vector2(0f, -15f);
                titleRect.sizeDelta = new Vector2(-40f, 40f);
            }

            TextMeshProUGUI titleText = titleObj.GetComponent<TextMeshProUGUI>();
            if (_gameFont != null) titleText.font = _gameFont;
            titleText.text = _isMathGate ? "SIGNAL COMBINATOR — OPERATIONS" : "SIGNAL PROCESSOR — OPERATIONS";
            titleText.fontStyle = FontStyles.Bold;
            titleText.enableAutoSizing = false;
            titleText.fontSize = Mathf.Clamp(_cellSize.y * 0.2f, 18f, 40f);
            titleText.alignment = TextAlignmentOptions.MidlineLeft;
            titleText.enableWordWrapping = false;
            titleText.overflowMode = TextOverflowModes.Overflow;
            titleText.margin = new Vector4(12f, 0f, 0f, 0f);
            titleText.raycastTarget = false;
            titleText.color = palette != null ? palette.uiLineColor : new Color(0.35f, 0.85f, 1.0f);

            titleObj.transform.SetAsLastSibling();
        }

        private void CreateOperatorButton(BuildPartButton prefab, Transform parent, OpInfo op)
        {
            BuildPartButton originalLogic = UnityEngine.Object.Instantiate(prefab, parent, false);
            GameObject btnObj = originalLogic.gameObject;

            TMP_Text oldLabel = Traverse.Create(originalLogic).Field("text").GetValue<TMP_Text>();
            if (oldLabel != null)
                UnityEngine.Object.DestroyImmediate(oldLabel.gameObject);
            UnityEngine.Object.DestroyImmediate(originalLogic);

            foreach (RawImage raw in btnObj.GetComponentsInChildren<RawImage>(true))
                raw.gameObject.SetActive(false);

            UnityEngine.UI.Button button = btnObj.GetComponent<UnityEngine.UI.Button>();
            if (button != null)
            {
                button.onClick = new UnityEngine.UI.Button.ButtonClickedEvent();
                byte opId = op.Id;
                button.onClick.AddListener(() => SelectOperation(opId));
            }

            float h = _cellSize.y;
            float symbolMax = h * 0.34f;
            float nameMax = h * 0.17f;

            GameObject symbolObj = new GameObject("MoreMath_SymbolText", typeof(RectTransform), typeof(TextMeshProUGUI));
            symbolObj.transform.SetParent(btnObj.transform, false);

            RectTransform symRect = symbolObj.GetComponent<RectTransform>();
            symRect.anchorMin = new Vector2(0f, 0.42f);
            symRect.anchorMax = new Vector2(1f, 0.96f);
            symRect.offsetMin = new Vector2(4f, 0f);
            symRect.offsetMax = new Vector2(-4f, 0f);

            TextMeshProUGUI symText = symbolObj.GetComponent<TextMeshProUGUI>();
            symText.text = op.Symbol;
            symText.alignment = TextAlignmentOptions.Center;
            symText.fontStyle = FontStyles.Bold;
            symText.enableWordWrapping = false;
            symText.enableAutoSizing = true; 
            symText.fontSizeMax = symbolMax;
            symText.fontSizeMin = symbolMax * 0.45f;
            symText.raycastTarget = false;
            symText.color = new Color(0.35f, 0.85f, 1.0f);

            GameObject nameObj = new GameObject("MoreMath_NameText", typeof(RectTransform), typeof(TextMeshProUGUI));
            nameObj.transform.SetParent(btnObj.transform, false);

            RectTransform nameRect = nameObj.GetComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0f, 0.04f);
            nameRect.anchorMax = new Vector2(1f, 0.42f);
            nameRect.offsetMin = new Vector2(3f, 0f);
            nameRect.offsetMax = new Vector2(-3f, 0f);

            TextMeshProUGUI nameText = nameObj.GetComponent<TextMeshProUGUI>();
            if (_gameFont != null) nameText.font = _gameFont;
            nameText.text = op.Name;
            nameText.fontStyle = FontStyles.Bold;
            nameText.alignment = TextAlignmentOptions.Center;
            nameText.enableWordWrapping = true;
            nameText.enableAutoSizing = true;
            nameText.fontSizeMax = nameMax;
            nameText.fontSizeMin = nameMax * 0.75f;
            nameText.overflowMode = TextOverflowModes.Overflow;
            nameText.raycastTarget = false;
            nameText.color = Color.white;

            btnObj.SetActive(true);
        }

        private void SelectOperation(byte operationId)
        {
            if (_interactionService != null)
            {
                _interactionService.ChangeMathOperation(new MathOperationNetworkRequest
                {
                    ShipId = _currentPartContext.ShipId,
                    PartId = _currentPartContext.PartId,
                    OperationId = operationId
                });
            }
            Close();
        }

        private static void HideInputControls(GameObject root)
        {
            foreach (TMP_InputField input in root.GetComponentsInChildren<TMP_InputField>(true))
                input.gameObject.SetActive(false);
            foreach (TMP_Dropdown dropdown in root.GetComponentsInChildren<TMP_Dropdown>(true))
                dropdown.gameObject.SetActive(false);
            foreach (UnityEngine.UI.InputField input in root.GetComponentsInChildren<UnityEngine.UI.InputField>(true))
                input.gameObject.SetActive(false);
            foreach (UnityEngine.UI.Dropdown dropdown in root.GetComponentsInChildren<UnityEngine.UI.Dropdown>(true))
                dropdown.gameObject.SetActive(false);
        }

        private static void DumpHierarchy(Transform t, int depth)
        {
            RectTransform rt = t as RectTransform;
            string info = rt != null
                ? $" size={rt.rect.size} aMin={rt.anchorMin} aMax={rt.anchorMax} pos={rt.anchoredPosition}"
                : "";

            for (int i = 0; i < t.childCount; i++)
                DumpHierarchy(t.GetChild(i), depth + 1);
        }

        public void Close()
        {
            bool wasOpen = _isOpen;
            _isOpen = false;

            RestoreGameplayInputs();

            if (_window != null)
            {
                UnityEngine.Object.Destroy(_window);
                _window = null;
            }

            if (_menuTracker != null)
            {
                _menuTracker.UntrackMenu(this);
                _menuTracker = null;
            }

            if (wasOpen && _buildMenuToggled != null)
            {
                _buildMenuToggled.Raise(false);
                _buildMenuToggled = null;
            }
        }

        private void OnDestroy()
        {
            Close();
        }

        private void DisableGameplayInputs()
        {
            _disabledActionMaps.Clear();
            foreach (var pi in Resources.FindObjectsOfTypeAll<PlayerInput>())
            {
                if (pi == null || pi.actions == null) continue;

                foreach (var map in pi.actions.actionMaps)
                {
                    if (map.enabled && !map.name.Equals("UI", StringComparison.OrdinalIgnoreCase))
                    {
                        map.Disable();
                        _disabledActionMaps.Add(map);
                    }
                }
            }
        }

        private void RestoreGameplayInputs()
        {
            foreach (var map in _disabledActionMaps)
            {
                if (map != null)
                    map.Enable();
            }
            _disabledActionMaps.Clear();
        }
    }
}