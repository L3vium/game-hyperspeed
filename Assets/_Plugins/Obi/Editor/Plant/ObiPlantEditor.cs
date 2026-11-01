using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Obi{
	
	/**
	 * Custom inspector for ObiPlant components.
	 * Allows particle selection and constraint edition. 
	 * 
	 * Selection:
	 * 
	 * - To select a particle, left-click on it. 
	 * - You can select multiple particles by holding shift while clicking.
	 * - To deselect all particles, click anywhere on the object except a particle.
	 * 
	 * Constraints:
	 * 
	 * - To edit particle constraints, select the particles you wish to edit.
	 * - Constraints affecting any of the selected particles will appear in the inspector.
	 * - To add a new pin constraint to the selected particle(s), click on "Add Pin Constraint".
	 * 
	 */
	[CustomEditor(typeof(ObiPlant)), CanEditMultipleObjects] 
	public class ObiPlantEditor : Editor//ObiParticleActorEditor
	{


		/*[MenuItem("GameObject/3D Object/Obi/Obi Plant (fully set up)",false,4)]
		static void CreateObiPlant()
		{
			GameObject c = new GameObject("Obi Plant");
			Undo.RegisterCreatedObjectUndo(c,"Create Obi Plant");
            ObiSolver solver = c.AddComponent<ObiSolver>();
			ObiPlant plant = c.AddComponent<ObiPlant>();
			
			//plant.Solver = solver;
		}*/
		
		ObiPlant plant;
		int selectedGenerator = -1;

		static bool showRendering;
		static bool showStructure;
		static bool showTropisms;
		static bool showMaterials;
		static bool showGeneration;
		
		public  void OnEnable(){
			//base.OnEnable();
			plant = (ObiPlant)target;

			//particlePropertyNames.AddRange(new string[]{"Tear Resistance"});
		}
		
		public void OnDisable(){
		//base.OnDisable();
			EditorUtility.ClearProgressBar();
		}

		public void UpdateParticleEditorInformation(){
			
			/*for(int i = 0; i < plant.positions.Length; i++)
			{
				wsPositions[i] = plant.GetParticlePosition(i);
				wsOrientations[i] = plant.GetParticleOrientation(i);	
				facingCamera[i] = true;		
			}*/

		}
		
        /*protected override void SetPropertyValue(ObiActorBlueprintEditor.ParticleProperty property,int index, float value){
			/*if (index >= 0 && index < plant.invMasses.Length){
				switch(property){
					case PlantParticleProperty.Mass: 
							plant.invMasses[index] = 1.0f / Mathf.Max(value,0.00001f);
						break; 
					case PlantParticleProperty.RotationalMass: 
							plant.invRotationalMasses[index] = 1.0f / Mathf.Max(value,0.00001f);
						break;
					case PlantParticleProperty.Radius:
							plant.principalRadii[index] = Vector3.one * value;
						break;
					case PlantParticleProperty.Layer:
							//plant.phases[index] = Oni.MakePhase((int)value,plant.SelfCollisions?Oni.ParticlePhase.SelfCollide:0);
						break;
				}
			}*/
		//}
		
        /*protected override float GetPropertyValue(ObiActorBlueprintEditor.ParticleProperty property, int index){
			/*if (index >= 0 && index < plant.invMasses.Length){
				switch(property){
					case PlantParticleProperty.Mass:
						return 1.0f/plant.invMasses[index];
					case PlantParticleProperty.RotationalMass:
						return 1.0f/plant.invRotationalMasses[index];
					case PlantParticleProperty.Radius:
						return plant.principalRadii[index][0];
					case PlantParticleProperty.Layer:
						return Oni.GetGroupFromPhase(plant.phases[index]);
				}
			}*/
			/*return 0;
		}*/

		private void BakeMesh(){

			ObiRopeExtrudedRenderer extruded = plant.GetComponent<ObiRopeExtrudedRenderer>();
			ObiRopeMeshRenderer deformed = plant.GetComponent<ObiRopeMeshRenderer>();

			/*if (extruded != null && extruded.extrudedMesh != null){
				ObiEditorUtils.SaveMesh(extruded.extrudedMesh,"Save extruded mesh","plant mesh");
			}*/
			/*if (deformed != null && deformed.deformedMesh != null){
				ObiEditorUtils.SaveMesh(deformed.deformedMesh,"Save deformed mesh","plant mesh");
			}*/
		}

		public override void OnInspectorGUI() {
			
			serializedObject.Update();

			/*GUI.enabled = plant.Initialized;
			EditorGUI.BeginChangeCheck();
			editMode = GUILayout.Toggle(editMode,new GUIContent("Edit particles",Resources.Load<Texture2D>("EditParticles")),"LargeButton");
			if (EditorGUI.EndChangeCheck()){
				SceneView.RepaintAll();
			}
			GUI.enabled = true;			

			EditorGUILayout.LabelField("Status: "+ (plant.Initialized ? "Initialized":"Not initialized"));

			if (GUILayout.Button("Initialize")){
				if (!plant.Initialized){
					EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
					CoroutineJob job = new CoroutineJob();
					//routine = job.Start(plant.GeneratePhysicRepresentationForMesh());
					EditorCoroutine.ShowCoroutineProgressBar("Generating physical representation...",ref routine);
					EditorGUIUtility.ExitGUI();
				}else{
					if (EditorUtility.DisplayDialog("Actor initialization","Are you sure you want to re-initialize this actor?","Ok","Cancel")){
						EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
						CoroutineJob job = new CoroutineJob();
						//routine = job.Start(plant.GeneratePhysicRepresentationForMesh());
						EditorCoroutine.ShowCoroutineProgressBar("Generating physical representation...",ref routine);
						EditorGUIUtility.ExitGUI();
					}
				} 
			}

			GUI.enabled = plant.Initialized;*/

			GUILayout.BeginHorizontal();
			if (GUILayout.Button("Set Rest State")){
				Undo.RecordObject(plant, "Set rest state");
				//plant.PullDataFromSolver(ParticleData.POSITIONS | ParticleData.VELOCITIES);
			}
			if (GUILayout.Button("Bake Mesh")){
				BakeMesh();
			}
			GUILayout.EndHorizontal();

			GUI.enabled = true;	

			Editor.DrawPropertiesExcluding(serializedObject,"m_Script","generators");

			DrawPlantUI();
	
			// Apply changes to the serializedProperty
			if (GUI.changed){
				serializedObject.ApplyModifiedProperties();
			

				if (plant.instantGrowth)
					plant.InstantGrow();
			}
			
		}


		private void DrawPlantUI(){

			SerializedProperty gens = serializedObject.FindProperty("generators");

			DrawGenerators();

			if (DrawToolbar(gens))
				DrawSelectedGenerator(gens);
			
		}

		private void DrawGenerators(){

			EditorGUILayout.BeginVertical(GUI.skin.FindStyle("flow background"));
			GUILayout.Space(8);

			for (int i = 0; i < plant.generators.Count; ++i){
			
				ObiPlantGenerator gen = plant.generators[i];

				GUILayout.BeginHorizontal();
				GUILayout.Space(8+plant.GetGeneratorLevel(gen) * 32);

				GUIContent content = new GUIContent("Generator");
				switch(gen.type){
					case ObiPlantGenerator.GeneratorType.Leaves: content = new GUIContent(gen.name, Resources.Load<Texture2D>("LeafButton")); break;
					case ObiPlantGenerator.GeneratorType.Branch: content = new GUIContent(gen.name, Resources.Load<Texture2D>("BranchButton")); break;
					case ObiPlantGenerator.GeneratorType.Trunk: content = new GUIContent(gen.name, Resources.Load<Texture2D>("TrunkButton")); break;
				}
	
				if (GUILayout.Toggle(i == selectedGenerator,content,"Button",GUILayout.Height(32),GUILayout.Width(100))){
					selectedGenerator = i;
				}
			  	GUILayout.EndHorizontal();
				
			}
			GUILayout.Space(8);
			EditorGUILayout.EndVertical();
		}

		private bool DrawToolbar(SerializedProperty gens){

			bool validSelection = selectedGenerator >= 0 && selectedGenerator < gens.arraySize;

			EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
			GUILayout.FlexibleSpace();

			GUI.enabled = plant.generators.Count == 0 || validSelection;

			if (GUILayout.Button(new GUIContent(Resources.Load<Texture2D>("TrunkButton"),"Add trunk generator"),EditorStyles.toolbarButton,GUILayout.Width(32))){
				Undo.RecordObject(plant, "Add generator");
				selectedGenerator = plant.AddGenerator(ObiPlantGenerator.GeneratorType.Trunk,"Trunk",validSelection ? selectedGenerator : -1);
				serializedObject.Update();
			}
			if (GUILayout.Button(new GUIContent(Resources.Load<Texture2D>("BranchButton"),"Add branch generator"),EditorStyles.toolbarButton,GUILayout.Width(32))){
				Undo.RecordObject(plant, "Add generator");
				selectedGenerator = plant.AddGenerator(ObiPlantGenerator.GeneratorType.Branch,"Branch",validSelection ? selectedGenerator : -1);
				serializedObject.Update();
			}
			if (GUILayout.Button(new GUIContent(Resources.Load<Texture2D>("LeafButton"),"Add trunk generator"),EditorStyles.toolbarButton,GUILayout.Width(32))){
				Undo.RecordObject(plant, "Add generator");
				selectedGenerator = plant.AddGenerator(ObiPlantGenerator.GeneratorType.Leaves,"Leaves",validSelection ? selectedGenerator : -1);
				serializedObject.Update();
			}

			GUI.enabled = validSelection;

			if (GUILayout.Button(new GUIContent(Resources.Load<Texture2D>("RemoveControlPoint"),"Remove generator"),EditorStyles.toolbarButton,GUILayout.Width(32))){
				Undo.RecordObject(plant, "Remove generator");

				plant.RemoveGenerator(selectedGenerator);
				selectedGenerator--;
 				validSelection = selectedGenerator >= 0 && selectedGenerator < gens.arraySize;
				serializedObject.Update();
			}

			EditorGUILayout.EndVertical();

			GUI.enabled = true;

			return validSelection;
		}

		private bool StartGeneratorSection(bool toggle,string title){
			GUILayout.BeginVertical(EditorStyles.helpBox);
			EditorGUI.indentLevel++;
			return EditorGUILayout.Foldout(toggle, title);
		}

		private void EndGeneratorSection(){
			EditorGUI.indentLevel--;
			EditorGUILayout.EndVertical();
		}

		private void DrawSelectedGenerator(SerializedProperty gens){

			// SELECTED GENERATOR:
			SerializedProperty gen = gens.GetArrayElementAtIndex(selectedGenerator);

			SerializedProperty branchingProb = gen.FindPropertyRelative("branchingProbability");
			SerializedProperty branchingCurve = gen.FindPropertyRelative("branchingCurve");

			if (plant.generators[selectedGenerator].type == ObiPlantGenerator.GeneratorType.Trunk || 
				plant.generators[selectedGenerator].type == ObiPlantGenerator.GeneratorType.Branch){

				showStructure = StartGeneratorSection(showStructure, "Structure");
       			if (showStructure){
					EditorGUILayout.CurveField(gen.FindPropertyRelative("shape"),Color.green,new Rect(0,0,1,1));
					EditorGUILayout.PropertyField(gen.FindPropertyRelative("length"));
					EditorGUILayout.PropertyField(gen.FindPropertyRelative("thickness"));
					EditorGUILayout.PropertyField(gen.FindPropertyRelative("resolution"));
					EditorGUILayout.PropertyField(gen.FindPropertyRelative("growthRate"));
					EditorGUILayout.PropertyField(gen.FindPropertyRelative("randomness"));
					EditorGUILayout.PropertyField(gen.FindPropertyRelative("color"));
				}
				EndGeneratorSection();

				showTropisms = StartGeneratorSection(showTropisms, "Tropisms");
       			if (showTropisms){
					EditorGUILayout.PropertyField(gen.FindPropertyRelative("phototropism"));
					EditorGUILayout.PropertyField(gen.FindPropertyRelative("lightGrowthBoost"));
					EditorGUILayout.PropertyField(gen.FindPropertyRelative("thigmotropism"));
				}
				EndGeneratorSection();

				showMaterials = StartGeneratorSection(showMaterials, "Material");
       			if (showMaterials){
					EditorGUILayout.PropertyField(gen.FindPropertyRelative("massMultiplier"));
					EditorGUILayout.PropertyField(gen.FindPropertyRelative("rigidness"));
				}
				EndGeneratorSection();

				showRendering = StartGeneratorSection(showRendering, "Rendering");
       			if (showRendering){
					EditorGUILayout.PropertyField(gen.FindPropertyRelative("smoothness"));
					EditorGUILayout.PropertyField(gen.FindPropertyRelative("curvatureDetail"));
					EditorGUILayout.PropertyField(gen.FindPropertyRelative("perimeterDetail"));
					EditorGUILayout.PropertyField(gen.FindPropertyRelative("thicknessScale"));
					EditorGUILayout.PropertyField(gen.FindPropertyRelative("uvScale"));
				}
				EndGeneratorSection();
			}	

			if (plant.generators[selectedGenerator].type == ObiPlantGenerator.GeneratorType.Branch){

				showGeneration = StartGeneratorSection(showGeneration, "Generation");
       			if (showGeneration){
					EditorGUILayout.PropertyField(gen.FindPropertyRelative("frequency"));
					EditorGUILayout.PropertyField(gen.FindPropertyRelative("multiplicity"));
	
					EditorGUILayout.BeginHorizontal();
					branchingProb.floatValue = EditorGUILayout.Slider("Branching probability",branchingProb.floatValue,0,1);
					branchingCurve.animationCurveValue = EditorGUILayout.CurveField(branchingCurve.animationCurveValue,Color.green,new Rect(0,0,1,1),GUILayout.MaxWidth(48));
	            	EditorGUILayout.EndHorizontal();
	
					EditorGUILayout.PropertyField(gen.FindPropertyRelative("spread"));
					EditorGUILayout.PropertyField(gen.FindPropertyRelative("twirl"));
					EditorGUILayout.PropertyField(gen.FindPropertyRelative("growthAngle"));
					EditorGUILayout.PropertyField(gen.FindPropertyRelative("horizontalAlign"));
					EditorGUILayout.PropertyField(gen.FindPropertyRelative("verticalRandomness"));
				}
				EndGeneratorSection();
			}

			if (plant.generators[selectedGenerator].type == ObiPlantGenerator.GeneratorType.Leaves){

				showGeneration = StartGeneratorSection(showGeneration, "Generation");
       			if (showGeneration){
					EditorGUILayout.PropertyField(gen.FindPropertyRelative("frequency"));
					EditorGUILayout.PropertyField(gen.FindPropertyRelative("multiplicity"));
					EditorGUILayout.PropertyField(gen.FindPropertyRelative("spread"));
					EditorGUILayout.PropertyField(gen.FindPropertyRelative("twirl"));
					EditorGUILayout.PropertyField(gen.FindPropertyRelative("growthAngle"));
					EditorGUILayout.PropertyField(gen.FindPropertyRelative("horizontalAlign"));
					EditorGUILayout.PropertyField(gen.FindPropertyRelative("verticalRandomness"));
					EditorGUILayout.PropertyField(gen.FindPropertyRelative("distanceFromTrunk"));
				}
				EndGeneratorSection();

				showRendering = StartGeneratorSection(showRendering, "Rendering");
       			if (showRendering){
					EditorGUILayout.PropertyField(gen.FindPropertyRelative("leafType"));
					EditorGUILayout.PropertyField(gen.FindPropertyRelative("customLeaf"));
	
					EditorGUILayout.PropertyField(gen.FindPropertyRelative("leafScale"));
					EditorGUILayout.PropertyField(gen.FindPropertyRelative("sizeRange"));
					EditorGUILayout.PropertyField(gen.FindPropertyRelative("trunkScale"));
				}
				EndGeneratorSection();

			}
			EditorGUILayout.Space();	
		}
	}
}


