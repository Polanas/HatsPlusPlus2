using DuckGame;
using HarmonyLib;
using ImGuiNET;
using LanguageExt.SomeHelp;
using LanguageExt.UnsafeValueAccess;
using Microsoft.Xna.Framework;
using MoonSharp.Interpreter;
using MoonSharp.Interpreter.Interop;
using MoonSharp.Interpreter.Loaders;
using MoonSharp.Interpreter.Serialization;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Channels;
using System.Text;
using System.Threading.Tasks;
using Script = MoonSharp.Interpreter.Script;

namespace HatsPlusPlus;

enum ReflectType {
    Number = 1,
    String,
    Boolean,
    Array,
    Enum,
    Hashset,
    List,
    Enumerable,
    Other,
}

class ObjectUserdataDescriptor : IUserDataDescriptor {
    public string Name => "Object";

    public Type Type => typeof(object);

    public string AsString(object obj) {
        return "Object";
    }

    public DynValue Index(Script script, object obj, DynValue index, bool isDirectIndexing) {
        return DynValue.Nil;
    }

    public bool IsTypeCompatible(Type type, object obj) {
        return true;
    }

    public DynValue MetaIndex(Script script, object obj, string metaname) {
        throw new NotImplementedException();
    }

    public bool SetIndex(Script script, object obj, DynValue index, DynValue value, bool isDirectIndexing) {
        throw new NotImplementedException();
    }
}

public static class LuaUtils {
    public static void RegisterTypes(Script script, DataType dataType = DataType.Table) {
        RegisterType<TeamId>(script, dataType);
        RegisterType<TeamGen>(script, dataType);
        RegisterType<TeamHandle>(script, dataType);
        RegisterType<TeamFrame>(script, dataType);
        RegisterType<TeamsBitmap>(script, dataType);
        RegisterType<HatSprite>(script, dataType);
        RegisterType<AnimFrame>(script, dataType);
        RegisterType<Animation>(script, dataType);
        RegisterType<AnimType>(script, dataType);
        RegisterType<DepthHat>(script, dataType);
        RegisterType<HatId>(script, dataType);

        LoadVec2(script);
        LoadVec3(script);
        LoadIVector2(script);
        RegisterOptions(script);

        var converters = Script.GlobalOptions.CustomConverters;
        converters.SetClrToScriptCustomConversion<AnimFrame>((script, animFrame) => {
            return script.Call(script.Globals["animFrame"], animFrame.value, animFrame.delay);
        });
        converters.SetScriptToClrCustomConversion(DataType.Table, typeof(AnimFrame), (value) => {
            return new AnimFrame {
                value = (int)value.Table.Get("value").Number,
                delay = value.Table.Get("delay").Type == DataType.Number ? (int)value.Table.Get("delay").Number : None
            };
        });
    }

    public static void LoadDuck(Script script) {
        var converters = Script.GlobalOptions.CustomConverters;
        converters.SetClrToScriptCustomConversion<Duck>((script, duck) => {
            var duckValue = DynValue.NewTable(script);
            var table = duckValue.Table;
            table["objectUserData"] = UserData.Create(duck, new ObjectUserdataDescriptor());
            table["reflect"] = NewReflect(script, duck);
            table["type"] = () => "duck";
            table["offdir"] = duck.offDir;
            table["position"] = duck.position;
            table["jumping"] = duck.jumping;
            table["immobilized"] = duck.immobilized;
            table["swinging"] = duck.swinging;
            table["angle"] = duck.angle;
            table["angleDegrees"] = duck.angleDegrees;
            table["dead"] = duck.dead;
            table["depth"] = duck.depth.value;
            table["spriteFrame"] = duck.spriteFrame;
            table["velocity"] = duck.velocity;
            table["crouch"] = duck.crouch;
            //TODO: verify this is working
            table["trapped"] = duck._trappedInstance != null;
            table["sliding"] = duck.sliding;

            var profileValue = DynValue.NewTable(script);
            profileValue.Table["name"] = duck.profile?.name;
            table["profile"] = profileValue;

            if (duck.ragdoll == null) {
                table["ragdoll"] = DynValue.Nil;
            } else {
                var ragdollValue = DynValue.NewTable(script);
                ragdollValue.Table["position"] = duck.ragdoll.position;
                ragdollValue.Table["angle"] = duck.ragdoll.angle;
                ragdollValue.Table["angleDegrees"] = duck.ragdoll.angleDegrees;
                ragdollValue.Table["depth"] = duck.ragdoll.depth.value;

                var part1Value = DynValue.NewTable(script);
                var part2Value = DynValue.NewTable(script);
                var part3Value = DynValue.NewTable(script);

                part1Value.Table["position"] = duck.ragdoll.part1.position;
                part1Value.Table["angle"] = duck.ragdoll.part1.angle;
                part1Value.Table["angleDegrees"] = duck.ragdoll.part1.angleDegrees;
                part1Value.Table["depth"] = duck.ragdoll.part1.depth.value;

                part2Value.Table["position"] = duck.ragdoll.part2.position;
                part2Value.Table["angle"] = duck.ragdoll.part2.angle;
                part2Value.Table["angleDegrees"] = duck.ragdoll.part2.angleDegrees;
                part2Value.Table["depth"] = duck.ragdoll.part2.depth.value;

                part3Value.Table["position"] = duck.ragdoll.part3.position;
                part3Value.Table["angle"] = duck.ragdoll.part3.angle;
                part3Value.Table["angleDegrees"] = duck.ragdoll.part3.angleDegrees;
                part3Value.Table["depth"] = duck.ragdoll.part3.depth.value;

                ragdollValue.Table["part1"] = part1Value;
                ragdollValue.Table["part2"] = part2Value;
                ragdollValue.Table["part3"] = part3Value;

                table["ragdoll"] = ragdollValue;
            }

            return duckValue;
        });

        RegisterOption<Duck>(script);
    }

    public static void RegisterType<T>(Script script, DataType dataType = DataType.Table) {
        UserData.RegisterType<T>();
        RegisterOption<T>(script, dataType);
    }

    public static DynValue NewReflect(Script script, object obj) {
        var value = DynValue.NewTable(script);
        var table = value.Table;
        table["typeName"] = () => obj.GetType().Name;
        table["type"] = () => {
            return GetReflectType(obj);
        };
        table["field"] = (string name) => {
            var field = obj.GetType().GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            if (field == null) {
                return DynValue.Nil;
            }
            var value = field.GetValue(obj);
            if (value == null) {
                return DynValue.Nil;
            }
            return NewReflect(script, value);

        };
        table["property"] = (string name) => {
            var field = obj.GetType().GetProperty(name);
            if (field == null) {
                return DynValue.Nil;
            }
            var value = field.GetValue(obj);
            if (value == null) {
                return DynValue.Nil;
            }
            return NewReflect(script, value);

        };
        table["objectUserData"] = UserData.Create(obj, new ObjectUserdataDescriptor());
        table["asNumber"] = () => {
            if (GetReflectType(obj) == ReflectType.Number) {
                return obj;
            }
            return DynValue.Nil;
        };
        table["asString"] = () => {
            if (GetReflectType(obj) == ReflectType.String) {
                return obj;
            }
            return DynValue.Nil;
        };
        table["asBoolean"] = () => {
            if (GetReflectType(obj) == ReflectType.Boolean) {
                return obj;
            }
            return DynValue.Nil;
        };
        table["asEnum"] = () => {
            if (GetReflectType(obj) == ReflectType.Enum) {
                return DynValue.NewNumber((int)obj);
            }
            return DynValue.Nil;
        };
        table["asHashset"] = (string typeName) => {
            if (GetReflectType(obj) == ReflectType.Hashset) {
                return NewReflectHashset(script, obj, typeName);
            }
            return DynValue.Nil;
        };
        table["asEnumerable"] = (string typeName) => {
            if (GetReflectType(obj) == ReflectType.Enumerable) {
                return NewReflectEnumerable(script, obj, typeName);
            }
            return DynValue.Nil;
        };
        return value;
    }

    static DynValue NewReflectEnumerable(Script script, object obj, string typeName) {
        var value = DynValue.NewTable(script);
        value.Table["count"] = () => {
            var typeOption = DGTypeByName(typeName);
            if (typeOption.Unwrap() is var genericType && typeOption.IsSome) { } else {
                return DynValue.Nil;
            }
            var enumerable = typeof(Enumerable);
            var count = enumerable.GetDeclaredMethods().Filter((m) => m.Name.Contains("Count")).ToList()[0].MakeGenericMethod(genericType);
            return DynValue.NewNumber((int)count.Invoke(null, new object[] { obj }));
        };
        value.Table["iter"] = (DynValue func) => {
            var typeOption = DGTypeByName(typeName);
            if (typeOption.Unwrap() is var genericType && typeOption.IsSome) { } else {
                return DynValue.Nil;
            }

            var type = obj.GetType();
            var enumerableType = typeof(IEnumerable<>).MakeGenericType(genericType);
            var getEnumerator = enumerableType.GetMethod("GetEnumerator", BindingFlags.Instance | BindingFlags.Public);
            var enumerator = (System.Collections.IEnumerator)getEnumerator.Invoke(obj, new object[] { });

            while (enumerator.MoveNext()) {
                var current = enumerator.Current;
                func.Function.Call(NewReflect(script, current));
            }

            return DynValue.Nil;
        };
        return value;
    }
    static DynValue NewReflectHashset(Script script, object obj, string typeName) {
        var value = DynValue.NewTable(script);
        value.Table["count"] = () => {
            var typeOption = DGTypeByName(typeName);
            if (typeOption.Unwrap() is var genericType && typeOption.IsSome) { } else {
                return DynValue.Nil;
            }
            var hashSetType = typeof(System.Collections.Generic.HashSet<>).MakeGenericType(genericType);
            var count = hashSetType.GetProperty("Count", BindingFlags.Public | BindingFlags.Instance);
            return count.GetValue(obj);
        };
        value.Table["iter"] = (DynValue func) => {
            var typeOption = DGTypeByName(typeName);
            if (typeOption.Unwrap() is var genericType && typeOption.IsSome) { } else {
                return DynValue.Nil;
            }

            var hashSetType = typeof(System.Collections.Generic.HashSet<>).MakeGenericType(genericType);
            var getEnumerator = hashSetType.GetMethod("GetEnumerator", BindingFlags.Instance | BindingFlags.Public);
            var enumerator = getEnumerator.Invoke(obj, new object[] {});

            var moveNext = enumerator.GetType().GetMethod("MoveNext", BindingFlags.Instance | BindingFlags.Public);
            var currentProperty = enumerator.GetType().GetProperty("Current", BindingFlags.Instance | BindingFlags.Public);

            while ((bool)moveNext.Invoke(enumerator, new object[] { })) {
                var current = currentProperty.GetValue(enumerator);
                func.Function.Call(NewReflect(script, current));
            }

            return DynValue.Nil;
        };
        return value;
    }

    static Option<Type> DGTypeByName(string typeName) {
        var DGAssembly = typeof(Duck).Assembly;
        var type = DGAssembly.GetType($"DuckGame.{typeName}");
        if (type == null) {
            LuaLogger.Log($"error: could not find type {typeName}");
            return None;
        }
        return type;
    }

    static ReflectType GetReflectType(object obj) {
        if (obj is sbyte 
            || obj is byte 
            || obj is short 
            || obj is ushort
            || obj is int 
            || obj is uint 
            || obj is long 
            || obj is ulong 
            || obj is float 
            || obj is double) {
            return ReflectType.Number;
        }
        if (obj is bool) {
            return ReflectType.Boolean;
        }
        if (obj is string) {
            return ReflectType.String;
        }
        if (obj.GetType().IsEnum) {
            return ReflectType.Enum;
        }
        if (obj.GetType().IsArray) {
            return ReflectType.Boolean;
        }
        if (obj.GetType().IsGenericType && obj.GetType().GetGenericTypeDefinition().IsAssignableFrom(typeof(System.Collections.Generic.List<>))) {
            return ReflectType.List;
        }
        if (obj.GetType().IsGenericType && obj.GetType().GetGenericTypeDefinition().IsAssignableFrom(typeof(System.Collections.Generic.HashSet<>))) {
            return ReflectType.Hashset;
        }
        if (obj.GetType().Name == "<CastIterator>d__97`1") {
            return ReflectType.Enumerable;
        }

        return ReflectType.Other;
    }

    public static void UpdateLevel(Script script) {
        //var level = script.Globals.Get("level").Table;
        //var current 
        //level["current"] = NewReflect(script, Level.current);
    }

    public static void LoadLevel(Script script) {
        var level = DynValue.NewTable(script).Table;
        level["nearest"] = (ScriptExecutionContext ctx, CallbackArguments args) => {
            var script = ctx.GetScript();
            var argsArray = args.GetArray();

            var typeName = argsArray[0].String;
            var pos = argsArray[1].ToObject<Vec2>();
            var ignoreOption = argsArray.Get(2);
            var typeOption = DGTypeByName(typeName);
            if (typeOption.Unwrap() is var type && typeOption.IsSome) { } else {
                return DynValue.Nil;
            }

            var levelType = typeof(Level);
            object[] arguments;
            Type[] types;
            if (ignoreOption.ValueUnsafe() is var ignore && ignoreOption.IsSome) {
                var objectUserData = ignore.Table.Get("objectUserData");
                arguments = new object[] { pos.x, pos.y, objectUserData.UserData.Object };
                types = new[] { typeof(float), typeof(float), typeof(Thing) };
            } else {
                types = new[] { typeof(Vec2) };
                arguments = new object[] { pos };
            }
            var nearest = levelType.GetMethod("Nearest", types);
            var nearestGeneric = nearest.MakeGenericMethod(type);
            var thing = nearestGeneric.Invoke(null, arguments);
            if (thing == null) {
                return DynValue.Nil;
            }
            return NewReflect(script, thing);
        };
        level["checkCircle"] = (string typeName, Vec2 position, float radius, Option<DynValue> ignoreOption) => {
            var typeOption = DGTypeByName(typeName);
            if (typeOption.Unwrap() is var type && typeOption.IsSome) { } else {
                return DynValue.Nil;
            }

            var levelType = typeof(Level);
            object[] arguments;
            Type[] types;
            if (ignoreOption.ValueUnsafe() is var ignore && ignoreOption.IsSome) {
                var objectUserData = ignore.Table.Get("objectUserData");
                arguments = new object[] { position, radius, objectUserData.UserData.Object };
                types = new[] { typeof(Vec2), typeof(float), typeof(Thing) };
            } else {
                arguments = new object[] { position, radius };
                types = new[] { typeof(Vec2), typeof(float) };
            }

            var checkCircle = levelType.GetMethod("CheckCircle", types);
            var checkCircleGeneric = checkCircle.MakeGenericMethod();
            var thing = checkCircleGeneric.Invoke(null, arguments);
            if (thing == null) {
                return DynValue.Nil;
            }
            return NewReflect(script, thing);
        };
        level["checkCircleAll"] = (string typeName, Vec2 position, float radius) => {
            var typeOption = DGTypeByName(typeName);
            if (typeOption.Unwrap() is var type && typeOption.IsSome) { } else {
                return DynValue.Nil;
            }

            var levelType = typeof(Level);
            object[] arguments = new object[] {position, radius};
            Type[] types = new[] {typeof(Vec2), typeof(float)};

            var method = levelType.GetMethod("CheckCircleAll", types);
            var methodGeneric = method.MakeGenericMethod(type);
            var things = methodGeneric.Invoke(null, arguments);
            if (things == null) {
                return DynValue.Nil;
            }
            return NewReflect(script, things);
        };
        var current = DynValue.NewTable(script);
        static DynValue Things(ScriptExecutionContext ctx, CallbackArguments args) {
            var script = ctx.GetScript();
            var typeName = args[1];
            if (typeName.String != null) {
                var typeOption = DGTypeByName(typeName.String);
                if (typeOption.Unwrap() is var type && typeOption.IsSome) { } else {
                    return DynValue.Nil;
                }

                var thingsOfType = NewReflect(script, Level.current.things[type]);
                var enumerable = thingsOfType.Table.Get("asHashset").Callback.Invoke(ctx, [DynValue.NewString("Thing")]);
                return enumerable;
            }
            var things = NewReflect(script,Level.current.things);
            var bigList  = things.Table.Get("field").Callback.Invoke(ctx, [DynValue.NewString("_bigList")]);
            return bigList.Table.Get("asHashset").Callback.Invoke(ctx, [DynValue.NewString("Thing")]);
        }
        var callback = DynValue.NewCallback(Things);
        current.Table["things"] = callback;
        level["current"] = current;
        script.Globals["level"] = level;
    }


    public static void RegisterOption<T>(Script script, DataType dataType = DataType.Table) {
        var converters = Script.GlobalOptions.CustomConverters;
        converters.SetClrToScriptCustomConversion<Option<T>>((script, option) => {
            if (option.ValueUnsafe() is var value && option.IsSome) {
                return DynValue.FromObject(script, value);
            }
            return DynValue.Nil;
        });
        converters.SetScriptToClrCustomConversion(dataType, typeof(Option<T>), (value) => {
            var obj = converters.GetScriptToClrCustomConversion(dataType, typeof(T)).Invoke(value);
            return Some((T)obj);
        });
        converters.SetScriptToClrCustomConversion(DataType.Nil, typeof(Option<T>), (value) => {
            return Option<T>.None;
        });
        converters.SetScriptToClrCustomConversion(DataType.Void, typeof(Option<T>), (value) => {
            return Option<T>.None;
        });
    }

    public static void RegisterOptions(Script script) {
        RegisterOption<float>(script);
        var converters = Script.GlobalOptions.CustomConverters;
        converters.SetScriptToClrCustomConversion(DataType.String, typeof(string), (value) => {
            return value.String;
        });
        RegisterOption<string>(script, DataType.String);

        converters.SetScriptToClrCustomConversion(DataType.Number, typeof(float), (value) => {
            return (float)value.Number;
        });
        RegisterOption<float>(script, DataType.Number);

        converters.SetScriptToClrCustomConversion(DataType.Number, typeof(int), (value) => {
            return (int)value.Number;
        });
        RegisterOption<int>(script, DataType.Number);

        converters.SetScriptToClrCustomConversion(DataType.Number, typeof(short), (value) => {
            return (short)value.Number;
        });
        RegisterOption<short>(script, DataType.Number);

        converters.SetScriptToClrCustomConversion(DataType.Number, typeof(ushort), (value) => {
            return (ushort)value.Number;
        });
        RegisterOption<ushort>(script, DataType.Number);

        converters.SetScriptToClrCustomConversion(DataType.Number, typeof(byte), (value) => {
            return (byte)value.Number;
        });
        RegisterOption<byte>(script, DataType.Number);

        converters.SetScriptToClrCustomConversion(DataType.Number, typeof(sbyte), (value) => {
            return (sbyte)value.Number;
        });
        RegisterOption<sbyte>(script, DataType.Number);

        converters.SetScriptToClrCustomConversion(DataType.Number, typeof(long), (value) => {
            return (long)value.Number;
        });
        RegisterOption<long>(script, DataType.Number);

        converters.SetScriptToClrCustomConversion(DataType.Number, typeof(ulong), (value) => {
            return (ulong)value.Number;
        });
        RegisterOption<ulong>(script, DataType.Number);

        converters.SetScriptToClrCustomConversion(DataType.Number, typeof(uint), (value) => {
            return (uint)value.Number;
        });
        RegisterOption<uint>(script, DataType.Number);

        converters.SetScriptToClrCustomConversion(DataType.Number, typeof(double), (value) => {
            return (double)value.Number;
        });
        RegisterOption<double>(script, DataType.Number);

        converters.SetScriptToClrCustomConversion(DataType.Boolean, typeof(bool), (value) => {
            return (bool)value.Boolean;
        });
        RegisterOption<bool>(script, DataType.Boolean);

        converters.SetClrToScriptCustomConversion<Option<DynValue>>((script, option) => {
            if (option.ValueUnsafe() is var value && option.IsSome) {
                return value;
            }
            return DynValue.Nil;
        });
        converters.SetScriptToClrCustomConversion(DataType.Table, typeof(Option<DynValue>), (value) => {
            return Some(value);
        });
        converters.SetScriptToClrCustomConversion(DataType.Nil, typeof(Option<DynValue>), (value) => {
            return Option<DynValue>.None;
        });
        converters.SetScriptToClrCustomConversion(DataType.Void, typeof(Option<DynValue>), (value) => {
            return Option<DynValue>.None;
        });
    }

    public static void OverridePrint(Script script) {
        static DynValue Print(ScriptExecutionContext ctx, CallbackArguments args) {
            var arguments = args.GetArray();
            var script = ctx.GetScript();
            StringBuilder log = new StringBuilder();
            foreach (var arg in arguments) {
                var argString = arg.IsNil() ? "nil" : script.FixedCall(script.Globals["tostring"], arg).String;
                log.Append(argString);
                log.Append("\t");
            }

            LuaLogger.Log(log.ToString());
            return DynValue.Nil;
        }
        script.Globals["print"] = DynValue.NewCallback(Print);
    }

    public static void LoadVec2(Script script) {
        var converters = Script.GlobalOptions.CustomConverters;
        converters.SetClrToScriptCustomConversion<Vec2>((script, vec) => {
            return script.Call(script.Globals["vec2"], vec.x, vec.y);
        });
        converters.SetScriptToClrCustomConversion(DataType.Table, typeof(Vec2), (value) => {
            return new Vec2((float)value.Table.RawGet(1).Number, (float)value.Table.RawGet(2).Number);
        });

        RegisterOption<Vec2>(script);
    }

    public static void LoadIVector2(Script script) {
        var converters = Script.GlobalOptions.CustomConverters;
        converters.SetClrToScriptCustomConversion<IVector2>((script, vec) => {
            return script.Call(script.Globals["vec2"], vec.X, vec.Y);
        });
        converters.SetScriptToClrCustomConversion(DataType.Table, typeof(IVector2), (value) => {
            return new IVector2((int)value.Table.RawGet(1).Number, (int)value.Table.RawGet(2).Number);
        });

        RegisterOption<IVector2>(script);
    }

    public static void LoadVec3(Script script) {
        var converters = Script.GlobalOptions.CustomConverters;
        converters.SetClrToScriptCustomConversion<Vec3>((script, vec) => {
            return script.Call(script.Globals["vec3"], vec.x, vec.y, vec.z);
        });
        converters.SetScriptToClrCustomConversion(DataType.Table, typeof(Vec3), (value) => {
            return new Vec3((float)value.Table.RawGet(1).Number, (float)value.Table.RawGet(2).Number,(float)value.Table.RawGet(3).Number);
        });

        RegisterOption<Vec3>(script);
    }

    public static void LoadApi(Script script) {
        var converters = Script.GlobalOptions.CustomConverters;
        (script.Options.ScriptLoader as ScriptLoaderBase).ModulePaths = [Path.Combine(Mod.GetPath<HatsPlusPlus2>("LuaScripts"), "?.lua")];
        OverridePrint(script);
        RegisterTypes(script);
        LoadMaths(script);
        LoadHatFunctions(script);
        LoadImgui(script);
        LoadInput(script);
        LoadLevel(script);
        LoadKeyboard(script);
        LoadDuck(script);

        converters.SetClrToScriptCustomConversion<GameTime>((script, gameTime) => {
            var table = DynValue.NewTable(script);
            table.Table["delta"] = (float)gameTime.ElapsedGameTime.TotalSeconds;
            table.Table["total"] = (float)gameTime.TotalGameTime.TotalSeconds;
            return table;
        });
        script.DoFile(Mod.GetPath<HatsPlusPlus2>(Path.Combine("LuaScripts", "main.lua")));
    }

    public static void LoadImgui(Script script) {
        var imguiFns = DynValue.NewTable(script);
        imguiFns.Table["window"] = (string name, DynValue func) => {
            ImGui.Begin(name);
            try {
                func.Function.Call();
            }
            catch (ScriptRuntimeException e) {
                LuaLogger.Log($"Error: {e.DecoratedMessage ?? e.Message}");
            }
            func.Function.Call();
            ImGui.End();
        };
        imguiFns.Table["button"] = (string text) => {
            return ImGui.Button(text);
        };
        imguiFns.Table["text"] = (string text) => {
            ImGui.Text(text);
        };
        script.Globals["imguiFns"] = imguiFns.Table;
    }

    public static void LoadHatFunctions(Script script) {
        script.Globals["vanillaHat"] = (TeamsBitmap teamsBitmap) => {
            var hatTable = DynValue.NewTable(script);
            var vanillaHat = (VanillaHat)Hats.Add(VanillaHat.New(teamsBitmap));

            hatTable.Table["sprite"] = vanillaHat.sprite;
            hatTable.Table["id"] = vanillaHat.GetId();
            hatTable.Table["teamsBitmap"] = teamsBitmap;

            hatTable.Table["getPosition"] = () => {
                return vanillaHat.hat.position;
            };
            hatTable.Table["setPosition"] = (Vec2 pos) => {
                vanillaHat.hat.position = pos;
            };
            hatTable.Table["getAngle"] = () => {
                return vanillaHat.hat.angleDegrees;
            };
            hatTable.Table["setAngle"] = (float angle) => {
                vanillaHat.hat.angleDegrees = angle;
            };
            hatTable.Table["getFlippedHorizontally"] = () => {
                return vanillaHat.hat.flipHorizontal;
            };
            hatTable.Table["setFlippedHorizontally"] = (bool flip) => {
                vanillaHat.hat.flipHorizontal = flip;
            };
            hatTable.Table["setStrappedOn"] = (bool value) => {
                vanillaHat.hat.strappedOn = value;
            };
            hatTable.Table["getStrappedOn"] = () => {
                return vanillaHat.hat.strappedOn;
            };
            hatTable.Table["equip"] = (DynValue value) => {
                var duck = (Duck)value.Table.Get("objectUserData").UserData.Object;
                duck.Equip(vanillaHat.hat, false);
            };
            hatTable.Table["unequip"] = () => {
                vanillaHat.hat.UnEquip();
            };
            hatTable.Table["isAlive"] = () => {
                return vanillaHat.IsAlive();
            };
            hatTable.Table["remove"] = () => {
                Hats.Remove(vanillaHat.GetId());
            };
            return hatTable;
        };
        script.Globals["teamsBitmap"] = (string path, Vec2 frameSize) => {
            var bitmap = Bitmap.FromPath(path);
            var either = TeamsStorage.LoadTeamsBitmap(bitmap, new IVector2((int)frameSize.x, (int)frameSize.y));
            return either.Match(
                (error) => {
                    LuaLogger.Log($"Error while loading teamsBitmap with path {path}: {error}");
                    return DynValue.Nil;
                },
                (bitmap) => DynValue.FromObject(script, bitmap)
            );
        };
        script.Globals["hatSprite"] = () => {
            return HatSprite.New();
        };
        script.Globals["animation"] = (string name, float delay, bool looping, List<AnimFrame> frames) => {
            return Animation.New(name, delay, looping, frames);
        };

        script.Globals["depthHat"] = (TeamsBitmap teamsBitmap) => {
            static DynValue RemoveFn(ScriptExecutionContext ctx, CallbackArguments args) {
                var hat = args.GetArray()[0];
                var id = hat.Table.Get("id").ToObject<HatId>();
                Hats.Remove(id);
                return DynValue.Nil;
            }
            static DynValue IsAliveFn(ScriptExecutionContext ctx, CallbackArguments args) {
                var hat = args.GetArray()[0];
                var id = hat.Table.Get("id").ToObject<HatId>();
                return DynValue.Nil;
            }
            static DynValue SetStateFn(ScriptExecutionContext ctx, CallbackArguments args) {
                var argsArray = args.GetArray();
                var hatTable = argsArray[0].Table;
                var state = argsArray[1].Number;
                hatTable["state"] = state;
                return DynValue.Nil;
            }
            static DynValue UpdateFn(ScriptExecutionContext ctx, CallbackArguments args) {
                var script = ctx.GetScript();
                var hatTable = args.GetArray()[0];
                var id = hatTable.Table.Get("id").ToObject<HatId>();
                if (Hats.Get(id).ValueUnsafe() is var hat && hat is not null) { } else {
                    return DynValue.Nil;
                }

                var state = hatTable.Table.Get("state");
                (hat as DepthHat).SetState(state.Type == DataType.Number ? (DepthHatState)state.Number : DepthHatState.Regular);
                hat.position = hatTable.Table.Get("position").ToObject<Vec2>();
                hat.depth = (float)hatTable.Table.Get("depth").Number;
                hat.angle = (float)hatTable.Table.Get("angle").Number;
                hat.flippedHorizontally = hatTable.Table.Get("flippedHorizontally").Boolean;
                hat.teamsBitmap = hatTable.Table.Get("teamsBitmap").ToObject<TeamsBitmap>();
                hat.sprite = hatTable.Table.Get("sprite").ToObject<HatSprite>();

                return DynValue.Nil;
            }
            var hat = Hats.Add(DepthHat.New(teamsBitmap, None));
            var hatTable = DynValue.NewTable(script).Table;

            hatTable.Set("position", DynValue.FromObject(script, hat.position));
            hatTable.Set("depth", DynValue.NewNumber(hat.depth));
            hatTable.Set("angle", DynValue.NewNumber(hat.angle));
            hatTable.Set("flippedHorizontally", DynValue.NewBoolean(hat.flippedHorizontally));
            hatTable.Set("teamsBitmap", DynValue.FromObject(script, teamsBitmap));
            hatTable.Set("sprite", DynValue.FromObject(script, hat.sprite));
            hatTable.Set("id", DynValue.FromObject(script, hat.GetId()));

            hatTable.Set("isAlive", DynValue.NewCallback(IsAliveFn));
            hatTable.Set("remove", DynValue.NewCallback(RemoveFn));
            hatTable.Set("update", DynValue.NewCallback(UpdateFn));
            hatTable.Set("setState", DynValue.NewCallback(SetStateFn));
            return hatTable;
        };

        script.Globals["depthAnimHat"] = (TeamsBitmap bitmap) => {
            var hat = Hats.Add(DepthAnimHat.New(bitmap, None));
            var hatTable = DynValue.NewTable(script).Table;
            static DynValue RemoveFn(ScriptExecutionContext ctx, CallbackArguments args) {
                var hat = args.GetArray()[0];
                var id = hat.Table.Get("id").ToObject<HatId>();
                Hats.Remove(id);
                return DynValue.Nil;
            }
            static DynValue IsAliveFn(ScriptExecutionContext ctx, CallbackArguments args) {
                var hat = args.GetArray()[0];
                var id = hat.Table.Get("id").ToObject<HatId>();
                return DynValue.Nil;
            }
            static DynValue UpdateFn(ScriptExecutionContext ctx, CallbackArguments args) {
                var script = ctx.GetScript();
                var hatTable = args.GetArray()[0];
                var id = hatTable.Table.Get("id").ToObject<HatId>();
                if (Hats.Get(id).ValueUnsafe() is var hat && hat is not null) { } else {
                    return DynValue.Nil;
                }

                hat.position = hatTable.Table.Get("position").ToObject<Vec2>();
                hat.depth = (float)hatTable.Table.Get("depth").Number;
                hat.angle = (float)hatTable.Table.Get("angle").Number;
                hat.flippedHorizontally = hatTable.Table.Get("flippedHorizontally").Boolean;
                hat.teamsBitmap = hatTable.Table.Get("teamsBitmap").ToObject<TeamsBitmap>();
                hat.sprite = hatTable.Table.Get("sprite").ToObject<HatSprite>();

                return DynValue.Nil;
            }
            hatTable.Set("position", DynValue.FromObject(script, hat.position));
            hatTable.Set("depth", DynValue.NewNumber(hat.depth));
            hatTable.Set("angle", DynValue.NewNumber(hat.angle));
            hatTable.Set("flippedHorizontally", DynValue.NewBoolean(hat.flippedHorizontally));
            hatTable.Set("teamsBitmap", DynValue.FromObject(script, bitmap));
            hatTable.Set("sprite", DynValue.FromObject(script, hat.sprite));
            hatTable.Set("id", DynValue.FromObject(script, hat.GetId()));

            hatTable.Set("isAlive", DynValue.NewCallback(IsAliveFn));
            hatTable.Set("remove", DynValue.NewCallback(RemoveFn));
            hatTable.Set("update", DynValue.NewCallback(UpdateFn));
            return hatTable;
        };
    }

    public static void LoadInput(Script script) {
        var input = DynValue.NewTable(script).Table;
        input["pressed"] = (string trigger, Option<string> profile) => Input.Pressed(trigger, profile.ValueOr("Any"));
        input["down"] = (string trigger, Option<string> profile) => Input.Down(trigger, profile.ValueOr("Any"));
        input["released"] = (string trigger, Option<string> profile) => Input.Released(trigger, profile.ValueOr("Any"));

        script.Globals["input"] = input;
    }

    public static void LoadKeyboard(Script script) {
        var keyboard = DynValue.NewTable(script).Table;
        keyboard["nothingPressed"] = () =>
            Keyboard.NothingPressed();
        keyboard["pressed"] = (int key, Option<bool> any) => {
            return Keyboard.Pressed((Keys)key, any.ValueOr(false));
        };
        keyboard["released"] = (int key) => Keyboard.Released((Keys)key);
        keyboard["down"] = (int key) => Keyboard.Down((Keys)key);

        script.Globals["keyboard"] = keyboard;
    }

    public static void UpdateDucks(Script script) {
        Table ducks = script.Globals.Get("ducks").Table;
        if (ducks == null) {
            ducks = DynValue.NewTable(script).Table;
            script.Globals["ducks"] = ducks;
        }
        ducks["main"] = DuckNetwork.localProfile?.duck ?? Profiles.DefaultPlayer1.duck;

        var allDucksValue = DynValue.NewTable(script);
        var profiles = Profiles.active;
        for (int i = 0; i < profiles.Count; i++) {
            var profile = profiles[i];
            allDucksValue.Table[i] = profile.duck;
        }
    }

    public static void LoadMaths(Script script) {
        var mathsTable = DynValue.NewTable(script).Table;
        mathsTable["pointDirection"] = (Vec2 p1, Vec2 p2) => {
            return Maths.PointDirection(p1, p2);
        };

        script.Globals["maths"] = mathsTable;
    }

    public static void Update(Script script) {
        LuaUtils.UpdateDucks(script);
        LuaUtils.UpdateLevel(script);
        LuaUtils.UpdateMouse(script);
    }
    

    public static void UpdateMouse(Script script) {
        Table mouse = script.Globals.Get("mouse").Table;
        if (mouse == null) {
            mouse = DynValue.NewTable(script).Table;
            script.Globals["mouse"] = mouse;
        }

        mouse["positionScreen"] = Mouse.positionScreen;
        mouse["left"] = (int)Mouse.left;
        mouse["right"] = (int)Mouse.right;
        mouse["middle"] = (int)Mouse.middle;
        mouse["scroll"] = Mouse.scroll;
        mouse["prevScrollDown"] = Mouse.prevScrollDown;
        mouse["prevScrollUp"] = Mouse.prevScrollUp;
        mouse["position"] = Mouse.position;
        mouse["mousePos"] = Mouse.mousePos;
        mouse["positionConsole"] = Mouse.positionConsole;
    }
}
