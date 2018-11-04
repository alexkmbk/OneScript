﻿using ScriptEngine.Machine.Contexts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Refl = System.Reflection;

namespace ScriptEngine.Machine
{
    public delegate IValue InstanceConstructor(IValue[] arguments);

    public class TypeFactory
    {
        private readonly Type _clrType;
        private Dictionary<int, InstanceConstructor> _constructorsCache = new Dictionary<int, InstanceConstructor>();

        public TypeFactory(Type clrType)
        {
            _clrType = clrType;
        }

        public InstanceConstructor GetConstructor(string typeName, IValue[] arguments)
        {
            if (_constructorsCache.TryGetValue(arguments.Length, out var constructor))
            {
                return constructor;
            }

            constructor = CreateConstructor(typeName, arguments);
            if(constructor != null)
                _constructorsCache[arguments.Length] = constructor;

            return constructor;

        }

        private InstanceConstructor CreateConstructor(string typeName, IValue[] arguments)
        {
            var definition = FindConstructor(arguments);
            if (definition == null)
                return null;

            var methodInfo = definition.Value.CtorInfo;
            var argsParam = Expression.Parameter(typeof(IValue[]), "args");
            var parameters = methodInfo.GetParameters();
            var argsToPass = new List<Expression>();

            if (definition.Value.Parametrized && parameters.Length > 0)
            {
                argsToPass.Add(Expression.Constant(typeName));
                parameters = parameters.Skip(1).ToArray();
            }

            int paramIndex = 0;
            for (int i = 0; i < arguments.Length; i++)
            {
                if (parameters[paramIndex].ParameterType.IsArray)
                {
                    // capture all

                    var copyMethod = typeof(TypeFactory).GetMethod("CaptureVariantArgs", Refl.BindingFlags.Static | Refl.BindingFlags.InvokeMethod | Refl.BindingFlags.NonPublic);
                    System.Diagnostics.Debug.Assert(copyMethod != null);

                    argsToPass.Add(Expression.Call(copyMethod, argsParam, Expression.Constant(i)));
                    break;
                }

                argsToPass.Add(Expression.ArrayIndex(argsParam, Expression.Constant(i)));
                ++paramIndex;

            }

            for (int i = paramIndex; i < parameters.Length; i++)
            {
                if (parameters[paramIndex].ParameterType.IsArray)
                {
                    argsToPass.Add(Expression.NewArrayBounds(typeof(IValue)));
                }
                else
                {
                    argsToPass.Add(Expression.Convert(Expression.Constant(parameters[i].DefaultValue), parameters[i].ParameterType));
                }
            }

            var constructorCallExpression = Expression.Call(methodInfo, argsToPass);
            var callLambda = Expression.Lambda<InstanceConstructor>(constructorCallExpression, argsParam).Compile();

            return callLambda;
        }

        internal static IValue[] CaptureVariantArgs(IValue[] sourceArgs, int startingFrom)
        {
            var newArray = new IValue[sourceArgs.Length - startingFrom];
            Array.Copy(sourceArgs, startingFrom, newArray, 0, newArray.Length);
            return newArray;
        }

        private ConstructorDefinition? FindConstructor(IValue[] arguments)
        {
            var ctors = _clrType.GetMethods(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public)
                            .Where(x => x.GetCustomAttributes(false).Any(y => y is ScriptConstructorAttribute))
                            .Select(x => new ConstructorDefinition
                            {
                                CtorInfo = x,
                                Parametrized = ((ScriptConstructorAttribute)x.GetCustomAttributes(typeof(ScriptConstructorAttribute), false)[0]).ParametrizeWithClassName
                            });


            int argCount = arguments.Length;
            foreach (var ctor in ctors)
            {
                var parameters = ctor.CtorInfo.GetParameters();

                if (ctor.Parametrized && parameters.Length > 0)
                {
                    if (parameters[0].ParameterType != typeof(string))
                    {
                        throw new InvalidOperationException("Type parametrized constructor must have first argument of type String");
                    }

                    parameters = parameters.Skip(1).ToArray();
                }

                bool success = (parameters.Length == 0 && argCount == 0)
                    || (parameters.Length > 0 && parameters[0].ParameterType.IsArray);

                if (success)
                    return ctor;

                if (parameters.Length > 0 && parameters.Length < argCount
                    && !parameters[parameters.Length - 1].ParameterType.IsArray)
                {
                    success = false;
                    continue;
                }

                for (int i = 0; i < parameters.Length; i++)
                {
                    if (parameters[i].ParameterType.IsArray)
                    {
                        // captures all remained args
                        success = true;
                        break;
                    }
                    else
                    {
                        if (i < argCount)
                        {
                            success = true;
                        }
                        else
                        {
                            if (parameters[i].IsOptional)
                            {
                                success = true;
                            }
                            else
                            {
                                success = false;
                                break; // no match
                            }
                        }
                    }
                }

                if (success)
                    return ctor;

            }

            return null;
        }

        private struct ConstructorDefinition
        {
            public Refl.MethodInfo CtorInfo { get; set; }
            public bool Parametrized { get; set; }
        }

    }

}
