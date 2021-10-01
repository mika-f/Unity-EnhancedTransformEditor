﻿/*-------------------------------------------------------------------------------------------
 * Copyright (c) Natsuneko. All rights reserved.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 *------------------------------------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using NatsunekoLaboratory.EnhancedTransformEditor.Reflection;

using UnityEditor;

using UnityEngine;

namespace NatsunekoLaboratory.EnhancedTransformEditor
{
    [CustomEditor(typeof(Transform))]
    [CanEditMultipleObjects]
    public class TransformExtendedInspector : Editor
    {
        private string _errors;
        private SerializedProperty _position;

        private Vector3<string> _positionExpression;
        private Vector3<float> _positionExpressionResult;
        private TransformRotationGUI _rotation;
        private Vector3<string> _rotationExpression;
        private Vector3<float> _rotationExpressionResult;
        private SerializedProperty _scale;
        private Vector3<string> _scaleExpression;
        private Vector3<float> _scaleExpressionResult;
        private bool _showEnhancedTransformEditor;
        private bool _showEnhancedTransformResult;

        public void OnEnable()
        {
            _position = serializedObject.FindProperty("m_LocalPosition");
            _scale = serializedObject.FindProperty("m_LocalScale");

            if (_rotation == null)
                _rotation = new TransformRotationGUI();
            _rotation.OnEnable(serializedObject.FindProperty("m_LocalRotation"), EditorGUIUtility.TrTextContent("Rotation", "The local rotation of this GameObject relative to parent."));

            _positionExpression = new Vector3<string>("this", "this", "this");
            _positionExpressionResult = new Vector3<float>(0, 0, 0);
            _scaleExpression = new Vector3<string>("this", "this", "this");
            _scaleExpressionResult = new Vector3<float>(0, 0, 0);
            _rotationExpression = new Vector3<string>("this", "this", "this");
            _rotationExpressionResult = new Vector3<float>(0, 0, 0);
        }

        public override void OnInspectorGUI()
        {
            if (!EditorGUIUtility.wideMode)
            {
                EditorGUIUtility.wideMode = true;
                EditorGUIUtility.labelWidth = EditorGUIUtility.currentViewWidth - 212;
            }

            serializedObject.Update();

            EditorGUILayout.PropertyField(_position, EditorGUIUtility.TrTextContent("Position", "The local position of this GameObject relative to the parent."));
            _rotation.RotationField();
            EditorGUILayout.PropertyField(_scale, EditorGUIUtility.TrTextContent("Scale", "The local scaling of this GameObject relative to the parent."));

            _showEnhancedTransformEditor = EditorGUILayout.Foldout(_showEnhancedTransformEditor, "Enhanced Transform Editor");
            if (_showEnhancedTransformEditor)
                InspectorEnhanced();


            var t = target as Transform;
            var position = t.position;
            if (Mathf.Abs(position.x) > 100000 || Mathf.Abs(position.y) > 100000 || Mathf.Abs(position.z) > 100000)
                EditorGUILayout.HelpBox(LocalizationDatabase.GetLocalizedString("Due to floating-point precision limitations, it is recommended to bring the world coordinates of the GameObject within a smaller range."), MessageType.Warning);

            serializedObject.ApplyModifiedProperties();
        }

        private void InspectorEnhanced()
        {
            EditorGUILayout.Space();

            EditorGUI.BeginChangeCheck();

            _positionExpression.X = EditorGUILayout.TextField("Position X", _positionExpression.X);
            _positionExpression.Y = EditorGUILayout.TextField("Position Y", _positionExpression.Y);
            _positionExpression.Z = EditorGUILayout.TextField("Position Z", _positionExpression.Z);

            EditorGUILayout.Space();

            _scaleExpression.X = EditorGUILayout.TextField("Scale X", _scaleExpression.X);
            _scaleExpression.Y = EditorGUILayout.TextField("Scale Y", _scaleExpression.Y);
            _scaleExpression.Z = EditorGUILayout.TextField("Scale Z", _scaleExpression.Z);

            EditorGUILayout.Space();

            _rotationExpression.X = EditorGUILayout.TextField("Rotation X", _rotationExpression.X);
            _rotationExpression.Y = EditorGUILayout.TextField("Rotation Y", _rotationExpression.Y);
            _rotationExpression.Z = EditorGUILayout.TextField("Rotation Z", _rotationExpression.Z);

            var testVariables = new List<(string, Union<float, object>)>
            {
                ("this", 0),
                ("index", 0),
                ("targets", targets.Select(_ => 0).ToArray()),
                ("objects", targets)
            };

            var testFunctions = new List<ExpressionEvaluator.CustomFunction>
            {
                new ExpressionEvaluator.CustomFunction("space_between", (_1, _2) => 0),
                new ExpressionEvaluator.CustomFunction("center", (_1, _2) => 0)
            };

            if (EditorGUI.EndChangeCheck())
            {
                var positions = new List<string>();
                if (!ExpressionEvaluator.TryEvaluate(_positionExpression.X, testVariables, testFunctions, out _))
                    positions.Add("Position.X");
                if (!ExpressionEvaluator.TryEvaluate(_positionExpression.Y, testVariables, testFunctions, out _))
                    positions.Add("Position.Y");
                if (!ExpressionEvaluator.TryEvaluate(_positionExpression.Z, testVariables, testFunctions, out _))
                    positions.Add("Position.Z");
                if (!ExpressionEvaluator.TryEvaluate(_scaleExpression.X, testVariables, testFunctions, out _))
                    positions.Add("Scale.X");
                if (!ExpressionEvaluator.TryEvaluate(_scaleExpression.Y, testVariables, testFunctions, out _))
                    positions.Add("Scale.Y");
                if (!ExpressionEvaluator.TryEvaluate(_scaleExpression.Z, testVariables, testFunctions, out _))
                    positions.Add("Scale.Z");
                if (!ExpressionEvaluator.TryEvaluate(_rotationExpression.X, testVariables, testFunctions, out _))
                    positions.Add("Rotation.X");
                if (!ExpressionEvaluator.TryEvaluate(_rotationExpression.Y, testVariables, testFunctions, out _))
                    positions.Add("Rotation.Y");
                if (!ExpressionEvaluator.TryEvaluate(_rotationExpression.Z, testVariables, testFunctions, out _))
                    positions.Add("Rotation.Z");

                var sb = new StringBuilder();
                foreach (var position in positions)
                {
                    if (sb.Length > 0)
                        sb.Append(", ");
                    sb.Append(position);
                }

                _errors = sb.ToString();
            }

            if (!string.IsNullOrWhiteSpace(_errors))
                EditorGUILayout.HelpBox($"Failed to compile expression in {_errors}", MessageType.Error);

            using (new EditorGUI.DisabledGroupScope(!string.IsNullOrWhiteSpace(_errors)))
            {
                if (GUILayout.Button("Test Transform"))
                {
                    _positionExpressionResult.X = ExpressionEvaluator.Evaluate(_positionExpression.X, testVariables, testFunctions);
                    _positionExpressionResult.Y = ExpressionEvaluator.Evaluate(_positionExpression.Y, testVariables, testFunctions);
                    _positionExpressionResult.Z = ExpressionEvaluator.Evaluate(_positionExpression.Z, testVariables, testFunctions);
                    _scaleExpressionResult.X = ExpressionEvaluator.Evaluate(_scaleExpression.X, testVariables, testFunctions);
                    _scaleExpressionResult.Y = ExpressionEvaluator.Evaluate(_scaleExpression.Y, testVariables, testFunctions);
                    _scaleExpressionResult.Z = ExpressionEvaluator.Evaluate(_scaleExpression.Z, testVariables, testFunctions);
                    _rotationExpressionResult.X = ExpressionEvaluator.Evaluate(_rotationExpression.X, testVariables, testFunctions);
                    _rotationExpressionResult.Y = ExpressionEvaluator.Evaluate(_rotationExpression.Y, testVariables, testFunctions);
                    _rotationExpressionResult.Z = ExpressionEvaluator.Evaluate(_rotationExpression.Z, testVariables, testFunctions);

                    _showEnhancedTransformResult = true;
                }

                if (GUILayout.Button("Apply Transform"))
                    ApplyTransform();

                _showEnhancedTransformResult = EditorGUILayout.Foldout(_showEnhancedTransformResult, "Show Result");
                if (_showEnhancedTransformResult)
                    using (new EditorGUI.DisabledGroupScope(true))
                    {
                        using (new EditorGUI.IndentLevelScope())
                        {
                            EditorGUILayout.TextField("Position X", _positionExpressionResult.X.ToString());
                            EditorGUILayout.TextField("Position Y", _positionExpressionResult.Y.ToString());
                            EditorGUILayout.TextField("Position Z", _positionExpressionResult.Z.ToString());

                            EditorGUILayout.Space();

                            EditorGUILayout.TextField("Scale X", _scaleExpressionResult.X.ToString());
                            EditorGUILayout.TextField("Scale Y", _scaleExpressionResult.Y.ToString());
                            EditorGUILayout.TextField("Scale Z", _scaleExpressionResult.Z.ToString());

                            EditorGUILayout.Space();

                            EditorGUILayout.TextField("Rotation X", _rotationExpressionResult.X.ToString());
                            EditorGUILayout.TextField("Rotation Y", _rotationExpressionResult.Y.ToString());
                            EditorGUILayout.TextField("Rotation Z", _rotationExpressionResult.Z.ToString());
                        }
                    }
            }
        }

        private void ApplyTransform()
        {
            var transforms = targets.Cast<Transform>().ToList();
            var targetsPosX = transforms.Select(w => w.position.x).ToArray();
            var targetsPosY = transforms.Select(w => w.position.y).ToArray();
            var targetsPosZ = transforms.Select(w => w.position.z).ToArray();
            var targetsRotX = transforms.Select(w => w.rotation.eulerAngles.x).ToArray();
            var targetsRotY = transforms.Select(w => w.rotation.eulerAngles.y).ToArray();
            var targetsRotZ = transforms.Select(w => w.rotation.eulerAngles.z).ToArray();
            var targetsSclX = transforms.Select(w => w.localScale.x).ToArray();
            var targetsSclY = transforms.Select(w => w.localScale.y).ToArray();
            var targetsSclZ = transforms.Select(w => w.localScale.z).ToArray();

            foreach (var (transform, index) in transforms.Select((w, i) => (w, i)))
            {
                transform.position = new Vector3
                {
                    x = ApplyTransform(_positionExpression.X, transform.position.x, index, targetsPosX, transforms.ToArray(), "x"),
                    y = ApplyTransform(_positionExpression.Y, transform.position.y, index, targetsPosY, transforms.ToArray(), "y"),
                    z = ApplyTransform(_positionExpression.Z, transform.position.z, index, targetsPosZ, transforms.ToArray(), "z")
                };

                var x = ApplyTransform(_rotationExpression.X, transform.rotation.eulerAngles.x, index, targetsRotX, transforms.ToArray(), "x");
                var y = ApplyTransform(_rotationExpression.Y, transform.rotation.eulerAngles.y, index, targetsRotY, transforms.ToArray(), "y");
                var z = ApplyTransform(_rotationExpression.Z, transform.rotation.eulerAngles.z, index, targetsRotZ, transforms.ToArray(), "z");
                transform.rotation = Quaternion.Euler(x, y, z);

                transform.localScale = new Vector3
                {
                    x = ApplyTransform(_scaleExpression.X, transform.localScale.x, index, targetsSclX, transforms.ToArray(), "x"),
                    y = ApplyTransform(_scaleExpression.Y, transform.localScale.y, index, targetsSclY, transforms.ToArray(), "y"),
                    z = ApplyTransform(_scaleExpression.Z, transform.localScale.z, index, targetsSclZ, transforms.ToArray(), "z")
                };

                Debug.Log(transform.localPosition);

                EditorUtility.SetDirty(transform);
            }
        }

        private static float ApplyTransform(string expression, float @this, int index, float[] targets, Transform[] objects, string attribute)
        {
            var variables = new List<(string, Union<float, object>)>
            {
                ("this", @this),
                ("index", index),
                ("__targets", targets),
                ("__objects", objects),
                ("__attribute", attribute)
            };

            var functions = new List<ExpressionEvaluator.CustomFunction>
            {
                // space_between(0.5, index) => space_between(space, index)
                new ExpressionEvaluator.CustomFunction("space_between", SpaceBetweenImpl),

                // center(0, index) => center(origin, index)
                new ExpressionEvaluator.CustomFunction("center", (args, vars) => 0.0f)
            };

            return ExpressionEvaluator.Evaluate(expression, variables, functions);
        }

        private static float GetAttributedValue(Vector3 vec, string attr)
        {
            switch (attr)
            {
                case "x":
                    return vec.x;

                case "y":
                    return vec.y;

                case "z":
                    return vec.z;

                default:
                    throw new ArgumentOutOfRangeException(attr);
            }
        }

        private static float SpaceBetweenImpl(float[] arguments, List<(string, Union<float, object>)> variables)
        {
            var space = arguments[0];
            var index = arguments[1];
            var targetTransforms = variables.First(w => w.Item1 == "__objects").Item2.AsT2 as Transform[];
            var attribute = variables.First(w => w.Item1 == "__attribute").Item2.AsT2 as string;
            var @default = variables.First(w => w.Item1 == "this").Item2.AsT1;
            if (targetTransforms == null)
                return @default;

            if (targetTransforms.Any(w => w.gameObject.GetComponent<MeshFilter>() == null)) 
                return @default;

            var meshes = targetTransforms.Select(w => w.gameObject.GetComponent<MeshFilter>()).ToList();
            var offset = 0.0f;
            for (var i = 0; i < index; i++)
            {
                var scale = GetAttributedValue(targetTransforms[i].localScale, attribute);
                if (i == 0)
                {
                    offset = GetAttributedValue((meshes[i].mesh.bounds.size - meshes[i].mesh.bounds.center) * scale, attribute) + space;
                    continue;
                }

                Debug.Log($"{targetTransforms[i]}: {GetAttributedValue(meshes[i].mesh.bounds.size * scale, attribute)}");
                offset += GetAttributedValue(meshes[i].mesh.bounds.size * scale, attribute) + space;
                
                // has next GameObject has scale?
                if (meshes.Count - 1 >= i + 1 && Mathf.Abs(GetAttributedValue(targetTransforms[i + 1].localScale, attribute) - 1) > Mathf.Epsilon)
                {
                    var nextScale = GetAttributedValue(targetTransforms[i + 1].localScale, attribute);
                    var nextBound = GetAttributedValue(meshes[i + 1].mesh.bounds.size, attribute);
                    var shimOffset = ((nextBound * nextScale) - nextBound) / 2;

                    Debug.Log($"NextBound: {nextBound}, NextScale: {nextScale}, Shims: {(nextBound * nextScale) - nextBound}");
                    offset += shimOffset;
                }
                else if (Mathf.Abs(scale - 1) > Mathf.Epsilon)
                {
                    var bound = GetAttributedValue(meshes[i].mesh.bounds.size, attribute);
                    offset -= ((bound * scale) - bound) / 2;
                }
            }

            return offset;
        }
    }
}