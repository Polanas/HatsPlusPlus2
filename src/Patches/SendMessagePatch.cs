using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using DuckGame;

namespace HatsPlusPlus;

//[HarmonyPatch(typeof(Send))]
//[HarmonyPatch("Message")]
//[HarmonyPatch(new Type[] {typeof(NetMessage), typeof(NetworkConnection) })]
//internal class SendMessagePatch {
//    static void Postfix(NetMessage msg, NetworkConnection who) {
//        if (Network.isActive && msg is NMRoomData roomData) {
//            var a = 10;
//        }
//    }
//}
