using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Xbim.Common;
using Xbim.Common.Step21;
using Xbim.Ifc;
using Xbim.IO;
using Xbim.Ifc4.ActorResource;
using Xbim.Ifc4.DateTimeResource;
using Xbim.Ifc4.ExternalReferenceResource;
using Xbim.Ifc4.PresentationOrganizationResource;
using Xbim.Ifc4.GeometricConstraintResource;
using Xbim.Ifc4.GeometricModelResource;
using Xbim.Ifc4.GeometryResource;
using Xbim.Ifc4.Interfaces;
using Xbim.Ifc4.Kernel;
using Xbim.Ifc4.MaterialResource;
using Xbim.Ifc4.MeasureResource;
using Xbim.Ifc4.ProductExtension;
using Xbim.Ifc4.ProfileResource;
using Xbim.Ifc4.PropertyResource;
using Xbim.Ifc4.QuantityResource;
using Xbim.Ifc4.RepresentationResource;
using Xbim.Ifc4.SharedBldgElements;
using Xbim.Ifc4.SharedBldgServiceElements;

namespace CreateDSMSystem
{
    class MyProgram
    {
        static int Main()
        {
            //first create and initialise a model called DSMSyetem
            Console.WriteLine("Initialising the IFC Project....");
            using (var model = CreateandInitModel("DSMSyetem"))
            {
                if (model != null)
                {
                    IfcBuilding building = CreateBuilding(model, "Jiangya Dam");
                    IfcDSMSystem DSMsystem = CreateDSMSystem(model, "DSM System for Jiangya Dam");
                    IfcMonitoringItem dssystem1 = CreateMonitoringItem(DSMsystem, 0, "Displacement Monitoring");
                    IfcMonitoringItem dssystem2 = CreateMonitoringItem(DSMsystem, 1, "Seepage Monitoring");

                    //AddToBuilding(model, system, building);

                    if (dssystem1 != null)
                    {
                        try
                        {
                            Console.WriteLine("Standard dssystem successfully created....");
                            //write the Ifc File
                            model.SaveAs("DSMSystemIfc4.ifc", StorageType.Ifc);
                            Console.WriteLine("DSMSystemIfc4.ifc has been successfully written");
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("Failed to save DSMSystem.ifc");
                            Console.WriteLine(e.Message);
                        }
                    }
                }
                else
                {
                    Console.WriteLine("Failed to initialise the model");
                }
            }
            Console.WriteLine("Press any key to exit to view the IFC file....");
            Console.ReadKey();
            LaunchNotepad("DSMSystemIfc4.ifc");
            return 0;

        }

        /// <summary>
        /// Write IFC file into notebook
        /// </summary>
        /// <param name="fileName">File name</param>
        private static void LaunchNotepad(string fileName)
        {
            Process p;
            try
            {

                p = new Process { StartInfo = { FileName = fileName, CreateNoWindow = false } };
                p.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception Occurred :{0},{1}",
                          ex.Message, ex.StackTrace);
            }
        }
        /// <summary>
        /// Create MonitoringItem
        /// </summary>
        /// <param name="DSMsystem">Related DSM system</param>
        /// <param name="type">Enumered Type</param>
        /// <param name="name">MonitoringItem Name</param>
        /// <returns></returns>
        private static IfcMonitoringItem CreateMonitoringItem(IfcDSMSystem DSMsystem, int type, string name)
        {
            var model = DSMsystem.Model;
            IfcMonitoringItem monitoringItem;
            IfcMonitoringItemEnum monitoringItemType = (IfcMonitoringItemEnum)type;
            using (var txn = model.BeginTransaction("MonitoringItem Creation"))
            {
                monitoringItem = model.Instances.New<IfcMonitoringItem>(s =>
                {
                    s.Name = name;
                    s.PredefinedType = monitoringItemType;
                });

                //把分布系统与系统联系起来
                var rag = model.Instances.OfType<IfcRelAssignsToGroup>().FirstOrDefault();
                if (rag == null)
                {
                    var rs = model.Instances.New<IfcRelAssignsToGroup>();
                    rs.RelatingGroup = DSMsystem;
                    rs.RelatedObjects.Add(monitoringItem);
                }
                else
                    rag.RelatedObjects.Add(monitoringItem);

                txn.Commit();
            }
            return monitoringItem;
        }

        /// <summary>
        /// Create DSMSystem
        /// </summary>
        /// <param name="model"></param>
        /// <param name="name">DSMSystem name</param>
        /// <returns></returns>
        private static IfcDSMSystem CreateDSMSystem(IfcStore model, string name)
        {
            using (var txn = model.BeginTransaction("Create DSMSystem"))
            {
                var system = model.Instances.New<IfcDSMSystem>();
                system.Name = name;

                //get the building there should only be one and it should exist
                var building = model.Instances.OfType<IfcBuilding>().FirstOrDefault();
                if (building != null)
                {
                    var rs = model.Instances.New<IfcRelServicesBuildings>();
                    rs.RelatingSystem = system;
                    rs.RelatedBuildings.Add(building);
                }

                txn.Commit();
                return system;

            }
        }

        /// <summary>
        /// Create a project and a dam 
        /// </summary>
        /// <param name="model"></param>
        /// <param name="name">Dam name</param>
        /// <returns></returns>
        private static IfcBuilding CreateBuilding(IfcStore model, string name)
        {
            using (var txn = model.BeginTransaction("Create Jiangya Dam"))
            {
                var building = model.Instances.New<IfcBuilding>();
                building.Name = name;

                building.CompositionType = IfcElementCompositionEnum.ELEMENT;
                //var localPlacement = model.Instances.New<IfcLocalPlacement>();
                //building.ObjectPlacement = localPlacement;
                //var placement = model.Instances.New<IfcAxis2Placement3D>();
                //localPlacement.RelativePlacement = placement;
                //placement.Location = model.Instances.New<IfcCartesianPoint>(p => p.SetXYZ(0, 0, 0));
                //get the project there should only be one and it should exist
                var project = model.Instances.OfType<IfcProject>().FirstOrDefault();
                if (project != null) project.AddBuilding(building);
              
                txn.Commit();
                return building;
            }
        }



        /// <summary>
        /// Sets up the basic parameters any model must provide, units, ownership etc
        /// </summary>
        /// <param name="projectName">Name of the project</param>
        /// <returns></returns>
        private static IfcStore CreateandInitModel(string projectName)
        {
            //first we need to set up some credentials for ownership of data in the new model
            var credentials = new XbimEditorCredentials
            {
                ApplicationDevelopersName = "Zhu Xi",
                ApplicationFullName = "Create DSM System for Jiangya Dam",
                ApplicationIdentifier = "DSMSystem.exe",
                ApplicationVersion = "1.0",
                EditorsFamilyName = "Zhu",
                EditorsGivenName = "Xi",
                EditorsOrganisationName = "xBimTeam"
            };
            //now we can create an IfcStore, it is in Ifc4 format and will be held in memory rather than in a database
            //database is normally better in performance terms if the model is large >50MB of Ifc or if robust transactions are required

            var model = IfcStore.Create(credentials, XbimSchemaVersion.Ifc4, XbimStoreType.InMemoryModel);

            //Begin a transaction as all changes to a model are ACID
            using (var txn = model.BeginTransaction("Initialise Model"))
            {

                //create a project
                var project = model.Instances.New<IfcProject>();
                //set the units to SI (mm and metres)
                //project.Initialize(ProjectUnits.SIUnitsUK);
                project.Name = projectName;
                //now commit the changes, else they will be rolled back at the end of the scope of the using statement
                txn.Commit();
            }
            return model;

        }

        private static IfcMonitoringSubItem CreateMonitoringSubItem(IfcDSMSystem DSMsystem, int type, string name)
        {
            var model = DSMsystem.Model;
            IfcMonitoringSubItem monitoringSubItem;
            IfcMonitoringSubItemEnum monitoringSubItemType = (IfcMonitoringSubItemEnum)type;
            using (var txn = model.BeginTransaction("MonitoringItem Creation"))
            {
                monitoringSubItem = model.Instances.New<IfcMonitoringSubItem>(s =>
                {
                    s.Name = name;
                    s.PredefinedType = monitoringSubItemType;
                });

                //把监测子项目与监测项目连接起来
                var rag = model.Instances.OfType<IfcRelAssignsToGroup>().FirstOrDefault();
                if (rag == null)
                {
                    var rs = model.Instances.New<IfcRelAssignsToGroup>();
                    rs.RelatingGroup = DSMsystem;
                    rs.RelatedObjects.Add(monitoringSubItem);
                }
                else
                    rag.RelatedObjects.Add(monitoringSubItem);

                txn.Commit();
            }
            return monitoringSubItem;
        }
    }
}
