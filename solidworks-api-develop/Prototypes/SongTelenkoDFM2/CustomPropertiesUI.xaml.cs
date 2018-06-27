﻿using AngelSix.SolidDna;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Windows.Controls;
using static AngelSix.SolidDna.SolidWorksEnvironment;
using static System.Windows.Visibility;
using SolidWorks.Interop.sldworks;
using System.Windows.Forms;
using System.Runtime.CompilerServices;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using static SongTelenkoDFM2.PythonIntegration;

namespace SongTelenkoDFM2
{    /// <summary>
    /// Interaction logic for CustomPropertiesUI.xaml
    /// </summary>
    public partial class CustomPropertiesUI : System.Windows.Controls.UserControl
    {
        #region Public Members

        public class FeatureToleranceObject
        {
            public string FeatureName { get; set; }
            public string FeatureTolerance { get; set; }
        }

        public List<FeatureToleranceObject> mFeatureTolerances = new List<FeatureToleranceObject>();

        #endregion

        #region Private Members

        /// <summary>
        /// Define private strings for custom properties buttons in CustomPropertiesUI.xaml
        /// </summary>

        // Notes
        private const string CustomPropertyNote1 = "Note1";
        private const string CustomPropertyNote2 = "Note2";

        // Design Recommendations
        private const string CustomPropertyRecommendation = "Recommendation";

        // Feature Tolerances
        private const string CustomPropertyFeatureTolerance = "Tolerance for ";

        #endregion

        #region Constructor

        /// <summary>
        /// Default constructor
        /// </summary>
        public CustomPropertiesUI()
        {
            DataContext = this;
            InitializeComponent();
            FeatureTolerance_Display.ItemsSource = mFeatureTolerances;
        }

        #endregion

        #region Startup

        /// <summary>
        /// Fired when the control is fully loaded
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UserControl_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            // By default show the no part open screen
            // and hide the analyze part and main content screens
            NoPartContent.Visibility = Visible;
            MainContent.Visibility = Hidden;

            // Listen out for the active model changing
            SolidWorksEnvironment.Application.ActiveModelInformationChanged += Application_ActiveModelInformationChanged;
        }

        #endregion

        #region Model Events

        /// <summary>
        /// Fired when the active SolidWorks model is changed
        /// </summary>
        /// <param name="obj"></param>
        private void Application_ActiveModelInformationChanged(Model obj)
        {
            ThreadHelpers.RunOnUIThread(() =>
            {

            });
            ReadDetails();
        }

        /// <summary>
        /// Reads all the details from the active model
        /// </summary>
        private void ReadDetails()
        {
            ThreadHelpers.RunOnUIThread(() =>
            {
                // Get the active model
                var model = SolidWorksEnvironment.Application.ActiveModel;

                // If we have no model, or the model is nor a part
                // then we show the No Part screen and return
                if (model == null || (!model.IsPart && !model.IsAssembly))
                {
                    // Show No Part screen
                    NoPartContent.Visibility = Visible;
                    MainContent.Visibility = Hidden;

                    return;
                }

                // If we got here, we have a part

                // Listen out for selection changes
                model.SelectionChanged += Model_SelectionChanged;

                // Show the main content
                NoPartContent.Visibility = Hidden;
                MainContent.Visibility = Visible;
                DesignCheckButton.IsEnabled = true;

                // Query all custom properties
                model.CustomProperties((properties) =>
                {
                    // Notes
                    NoteText1.Text = properties.FirstOrDefault(property => string.Equals(CustomPropertyNote1, property.Name, StringComparison.InvariantCultureIgnoreCase))?.Value;
                    NoteText2.Text = properties.FirstOrDefault(property => string.Equals(CustomPropertyNote2, property.Name, StringComparison.InvariantCultureIgnoreCase))?.Value;

                    // Design Recommendations
                    Recommendation1.Text = properties.FirstOrDefault(property => string.Equals(CustomPropertyRecommendation, property.Name, StringComparison.InvariantCultureIgnoreCase))?.Value;

                });

                // Mass
                MassText.Text = model.MassProperties?.MassInMetric();

                // Clear material selection
                List<Material> FilteredMaterials = GetMaterialList();
                RawMaterialList.ItemsSource = FilteredMaterials;
                RawMaterialList.DisplayMemberPath = "DisplayName";
                RawMaterialList.SelectedIndex = -1;

                // Select existing material
                var existingMaterial = model.GetMaterial();

                // If we have a material
                if (existingMaterial != null)
                    RawMaterialList.SelectedItem = FilteredMaterials?.FirstOrDefault(f => f.Database == existingMaterial.Database && f.Name == existingMaterial.Name);
            });
        }

        /// <summary>
        /// Checks for change in model selection
        /// This is what the user is clicking on (feature, face, drawing, dimension, etc)
        /// </summary>
        private void Model_SelectionChanged()
        {
            SolidWorksEnvironment.Application.ActiveModel?.SelectedObjects((objects) =>
            {
                
                ThreadHelpers.RunOnUIThread(() =>
                {
                                        
                });
            });
        }

        #endregion

        #region Button Events

        /// <summary>
        /// Called when the read button is clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ReadButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            ReadDetails();
        }

        /// <summary>
        /// Called when the reset button is clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ResetButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var model = SolidWorksEnvironment.Application.ActiveModel;
            var model2 = (ModelDoc2)SolidWorksEnvironment.Application.UnsafeObject.ActiveDoc;
            ModelDocExtension extension = model2.Extension;
            CustomPropertyManager propertyManager = default(CustomPropertyManager);
            propertyManager = extension.get_CustomPropertyManager("");

            model.CustomProperties((properties) =>
            {
                // Find all feature-tolerance custom properties
                List<CustomProperty> found = properties.FindAll( property => property.Name.Contains(CustomPropertyFeatureTolerance));

                // Delete each one
                foreach (CustomProperty item in found) propertyManager.Delete(item.Name);
            });

            mFeatureTolerances.Clear();
            FeatureTolerance_Display.Items.Refresh();

            RawMaterialList.SelectedIndex = -1;

            NoteText1.Text = string.Empty;
            NoteText2.Text = string.Empty;
            Recommendation1.Text = string.Empty;
        }

        /// <summary>
        /// Called when the apply button is clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ApplyButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var model = SolidWorksEnvironment.Application.ActiveModel;

            // Check if we have a part
            if (model == null || !model.IsPart)
                return;

            // Notes
            model.SetCustomProperty(CustomPropertyNote1, NoteText1.Text);
            model.SetCustomProperty(CustomPropertyNote2, NoteText2.Text);

            // Design Recommendations
            model.SetCustomProperty(CustomPropertyRecommendation, Recommendation1.Text);

            // Feature Tolerances
            for (int i = 0; i < mFeatureTolerances.Count; i++)
            {
                FeatureToleranceObject item = mFeatureTolerances.ElementAt(i);
                string CustomPropertyName = CustomPropertyFeatureTolerance + item.FeatureName;
                model.SetCustomProperty(CustomPropertyName, item.FeatureTolerance);
            }
            
            // If user does not have a material selected, clear it
            if (RawMaterialList.SelectedIndex < 0)
                model.SetMaterial(null);
            // Otherwise set the material to the selected one
            else
                model.SetMaterial((Material)RawMaterialList.SelectedItem);

            // Re-read details to confirm they are correct
            ReadDetails();
        }

        /// <summary>
        /// Get selected feature and ask user to provide specific tolerances
        /// </summary>
        private void DesignCheckButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            SolidWorksEnvironment.Application.ActiveModel?.SelectedObjects((objects) =>
            {
                Tolerance_Check();

                // Set the feature button text
                ThreadHelpers.RunOnUIThread(() =>
                {

                });
            });
        }
        
        /// <summary>
        /// Run automated SculptPrint python script
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ManufacturingCheck_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            string script_location = Path.Combine(this.AssemblyPath(), "Test.py");
            string res = Run_cmd(script_location, string.Empty);
            Debug.Print(res);
        }

        #endregion

        #region Private Helper Functions

        /// <summary>
        /// Asks user to specify tolerances for each feature
        /// </summary>
        private void Tolerance_Check()
        {
            // Clear the old tolerances
            mFeatureTolerances.Clear();

            var model = (ModelDoc2)SolidWorksEnvironment.Application.UnsafeObject.ActiveDoc;
            var selectionManager = model.SelectionManager;
            var featureManager = model.FeatureManager;

            var AllFeatures = (object[])featureManager.GetFeatures(false);

            List<List<Feature>> filteredFeatures = GetFeatureDictionary(AllFeatures);

            for (int i=0; i<filteredFeatures.Count; i++)
            {
                // Get the head feature of each feature-sketch-list
                Feature headFeature = (filteredFeatures[i]).First();

                // Get the dictionary of feature dimensions and values 
                //Dictionary<string, double> dims = GetDimensions(headFeature);

                // Highlight the feature
                model.Extension.SelectByID2(headFeature.Name, "BODYFEATURE", 0, 0, 0, false, 0, null, 0);

                // Get the user specified or default tolerance for this feature
                //string[] thisFeatureTolerance = GetFeatureTolerance(headFeature);
                FeatureToleranceObject thisFeatureTolerance = GetFeatureTolerance(headFeature);

                // Add this tolerance to the list of all tolerances
                mFeatureTolerances.Add(thisFeatureTolerance);

                FeatureTolerance_Display.Items.Refresh();
            }
        }

        /// <summary>
        /// Generate dictionary of user-specified tolerances
        /// </summary>
        /// <param name="feature"></param>
        /// <returns></returns>
        private FeatureToleranceObject GetFeatureTolerance(Feature feature)
        {
            // Default tolerance string
            string default_tolerance = "+/- 0.1mm";

            // Configure the message box to ask user if this feature is critical
            string messageBoxText = "Does the highlighted feature,\r\n" + feature.Name + ",\r\n need a very tight tolerance?";
            string formTitle = "Critical Features";

            // Use the custom FeatureCriticalMessageBox class to ask the user
            FeatureCriticalMessageBox isFeatureCritical = new FeatureCriticalMessageBox(messageBoxText, formTitle);
            DialogResult criticalFeature_result = isFeatureCritical.ShowDialog();

            // Based on the result, either ask the user for a more specific tolerance or use the default tolerance
            if (criticalFeature_result == DialogResult.Yes)
            {
                // User has identified this feature as critical, so it may require a tighter tolerance
                string messageBoxText2 = "The default tolerance is +/- 0.1mm. Would you like to change this value for " + feature.Name + "?";
                string formTitle2 = "Critical Feature Tolerance";

                // Display Tolerance Message Box to the user and wait for tolerance selection
                ToleranceMessageBox CriticalFeatureTolerance = new ToleranceMessageBox(messageBoxText2, formTitle2);
                DialogResult tolerance_result = CriticalFeatureTolerance.ShowDialog();

                string[] res = { feature.Name, CriticalFeatureTolerance.Tolerance_Value };

                FeatureToleranceObject result = new FeatureToleranceObject()
                {
                    FeatureName = feature.Name,
                    FeatureTolerance = CriticalFeatureTolerance.Tolerance_Value
                };

                return result;
            }
            // User has not identified this feature as critical, so return the default tolerance
            else
            {
                string[] res2 = { feature.Name, default_tolerance };

                FeatureToleranceObject result2 = new FeatureToleranceObject()
                {
                    FeatureName = feature.Name,
                    FeatureTolerance = default_tolerance
                };

                return result2;
            }
        }

        /// <summary>
        /// Determine if a feature is necessary
        /// </summary>
        /// <param name="feature"></param>
        /// <returns>The passed-in feature if it is necessary</returns>
        private Feature IsNecessaryFeature(object feature)
        {
            if (feature != null)
            {
                if (((Feature)feature).Name == "Comments") return null;
                else if (((Feature)feature).Name == "Favorites") return null;
                else if (((Feature)feature).Name == "History") return null;
                else if (((Feature)feature).Name == "Selection Sets") return null;
                else if (((Feature)feature).Name == "Sensors") return null;
                else if (((Feature)feature).Name == "Design Binder") return null;
                else if (((Feature)feature).Name == "Annotations") return null;
                else if (((Feature)feature).Name == "Surface Bodies") return null;
                else if (((Feature)feature).Name == "Solid Bodies") return null;
                else if (((Feature)feature).Name == "Lights, Cameras and Scene") return null;
                else if (((Feature)feature).Name == "Equations") return null;
                else if (((Feature)feature).GetTypeName2().Contains("Material")) return null;
                else if (((Feature)feature).Name == "Front Plane") return null;
                else if (((Feature)feature).Name == "Top Plane") return null;
                else if (((Feature)feature).Name == "Right Plane") return null;
                else if (((Feature)feature).Name == "Origin") return null;
                else if (((Feature)feature).Name.Contains("Notes")) return null;
                else if (((Feature)feature).Name == "Ambient") return null;
                else if (((Feature)feature).Name.Contains("Directional")) return null;
                else if (((Feature)feature).Name.Contains("Sketch")) return null;
                else
                {
                    return (Feature)feature;
                }
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Make a list of necessary features in insertion order
        /// Each value is a list of features containing the parent feature and the child sketches
        /// </summary>
        /// <param name="FeatureSet"></param>
        /// <returns></returns>
        private List<List<Feature>> GetFeatureDictionary(object[] FeatureSet)
        {
            // Start empty dictionary
            List<List<Feature>> featureDictionary = new List<List<Feature>>();

            // Check each feature in the unfiltered feature array that is passed in
            foreach (object feature in FeatureSet)
            {
                // Is the feature necessary?
                Feature featureIteration = IsNecessaryFeature(feature);
                
                // If it is, then find the feature's sketches
                if (featureIteration != null)
                {
                    // Start the feature list entry
                    List<Feature> entry = new List<Feature>();

                    // add the parent feature
                    entry.Add(featureIteration);

                    // Print parent feature's name
                    //Debug.Print("Parent feauture: " + featureIteration.Name);

                    // Find and add the sketches underlying the feature
                    List<Feature> sketches = GetSubFeatures(featureIteration);
                    if (sketches != null)
                    {
                        entry.AddRange(sketches);
                    }

                    // Add list of feature and sketches to the main featureDictionary list
                    featureDictionary.Add(entry);

                    //Debug.Print(" ");
                }
            }

            // return the main featureDictionary list
            return featureDictionary;
        }

        /// <summary>
        /// Get all levels of subfeatures of a given feature
        /// </summary>
        /// <param name="feature"></param>
        /// <returns></returns>
        private List<Feature> GetSubFeatures(Feature feature)
        {
            // Make an empty list of features
            List<Feature> sketches = new List<Feature>();

            // Find first subfeature
            var sketch = feature.GetFirstSubFeature();

            while (sketch != null)
            {
                // If the subfeature is not null, add it to the dictionary
                // Debug.Print("Child Sketch: " + ((Feature)sketch).Name);
                sketches.Add((Feature)sketch);

                // Find the next subfeature
                sketch = ((Feature)sketch).GetNextSubFeature();
            }

            if (sketches.Count == 0)
            {
                return null;
            } else {
                return sketches;
            }
        }

        /// <summary>
        /// Get all dimensions relating to a feature
        /// </summary>
        /// <param name="feature"></param>
        /// <returns></returns>
        private Dictionary<string, double> GetDimensions(Feature feature)
        {
            // Make an empty list of dimensions
            Dictionary<string, double> dims = new Dictionary<string, double>();

            // Find first dimension
            var displayDimension = (DisplayDimension)feature.GetFirstDisplayDimension();

            while (displayDimension != null)
            {
                // If the first display dimension is not null, add it to the list
                Dimension dimension = (Dimension)displayDimension.GetDimension();
                if (dimension.FullName.Contains("Angle")) {
                    //Debug.Print(dimension.FullName + " " + dimension.GetSystemValue2("") + "rads");
                } else {
                    //Debug.Print(dimension.FullName + " " + 1000.0 * dimension.GetSystemValue2("") + "mm");
                }
                
                dims.Add(dimension.FullName, dimension.GetSystemValue2(""));

                // Get the next display dimension
                displayDimension = (DisplayDimension)feature.GetNextDisplayDimension(displayDimension);
            }

            //Debug.Print(" ");

            if (dims.Count == 0)
            {
                return null;
            }
            else
            {
                return dims;
            }
        }

        /// <summary>
        /// Get novice-friendly materials list
        /// </summary>
        /// <returns></returns>
        private List<Material> GetMaterialList()
        {
            // Get all materials
            var materials = SolidWorksEnvironment.Application.GetMaterials();

            var filteredMaterials = new List<Material>();

            foreach (Material material in materials)
            {
                if (material.Name.Equals("ABS") | material.Name.Equals("AISI 304") | material.Name.Contains("Delrin") |
                    material.Name.Equals("6061-T6 (SS)") | material.Name.Contains("Acrylic (Medium-high impact)") |
                    material.Name.Contains("7075-T6 (SN)") | material.Name.Contains("Plain Carbon Steel"))
                {
                    filteredMaterials.Add(material);
                }
            }
            filteredMaterials = filteredMaterials.OrderBy(f => f.Classification).ToList();
            filteredMaterials.Insert(0, new Material { Name = "Remove Material", Classification = "Not specified", DatabaseFileFound = false });

            return filteredMaterials;
        }

        #endregion
    }
}