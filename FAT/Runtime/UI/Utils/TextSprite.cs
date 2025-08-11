using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using EL;
using FAT.Merge;
using fat.rawdata;
using TMPro;
using static fat.rawdata.CoinType;

namespace FAT {
    public static class TextSprite {
        public static readonly string Coin = "\U0000F001";
        public static readonly string CoinL = "\U0000F011";
        public static readonly string Diamond = "\U0000F002";
        public static readonly string Energy = "\U0000F003";
        public static readonly string Stone = "\U0000F004";
        public static readonly string Tile = "\U0000F005";
        public static readonly string Wood = "\U0000F006";
        public static readonly string Ceramics = "\U0000F007";
        public static readonly string Market = "\U0000F008";
        public static readonly string Score = "\U0000F009";

        public static string FromId(int id_) {
            
            return id_ switch {
                1 => Diamond,
                5 => Coin,
                //30 => Exp,
                31 => Energy,
                11 => Stone,
                18 => Tile,
                10 => Wood,
                14 => Ceramics,
                _ => string.Empty,
            };
        }

        public static string FromType(CoinType type_) {
            return type_ switch {
                MergeCoin => Coin,
                Gem => Diamond,
                ToolStone => Stone,
                ToolTile => Tile,
                ToolWood => Wood,
                ToolCeramics => Ceramics,
                _ => string.Empty,
            };
        }

        public static string FromName(string name_) {
            return @$"<sprite name=""{name_}"">";
        }

        public static string FromToken(int id_) {
            var conf = fat.conf.Data.GetObjToken(id_);
            return @$"<sprite name=""{conf?.SpriteName}"">";
        }
    }
}
