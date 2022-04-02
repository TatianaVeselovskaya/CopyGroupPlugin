using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CopyGroupPlugin
{
    [TransactionAttribute(TransactionMode.Manual)] 
    public class CopyGroup : IExternalCommand

    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
            {
            try
                {
                 UIDocument uiDoc = commandData.Application.ActiveUIDocument;
                 Document doc = uiDoc.Document;
                 // ссылки на докуметы

                GroupPickFilter groupPickFilter = new GroupPickFilter();
                Reference reference = uiDoc.Selection.PickObject(ObjectType.Element, groupPickFilter, "Выберите группу объектов");           
                Element element = doc.GetElement(reference);
                Group group = element as Group;
                XYZ groupCenter = GetElementCenter(group);
                Room room = GetRoomByPoint(doc, groupCenter);
                XYZ roomCenter = GetElementCenter(room);
                XYZ offset = groupCenter - roomCenter; // смещение комнаты относительно группы 
                // выбрали группу, обработали выбор

                XYZ point = uiDoc.Selection.PickPoint("Выберите точку");
                Room selectedRoom = GetRoomByPoint(doc, point);
                XYZ selectedRoomCenter = GetElementCenter(selectedRoom);
                XYZ pastePoint = selectedRoomCenter + offset;
                // размещение в новой комнате

                Transaction transaction = new Transaction(doc);
                transaction.Start("Копирование группы элементов");
                doc.Create.PlaceGroup(pastePoint, group.GroupType);
                transaction.Commit();
                }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            //Обработчик исключений, связанные с ESC
            {
                return Result.Cancelled; // возвращаем результат отмена
            }
            catch(Exception ex)
            //Обработчик исключений, все остальные ошибки
            {
                message = ex.Message; // передаем текст ошибки в параметр Message
                return Result.Failed;
            }

            return Result.Succeeded;
        }

        public XYZ GetElementCenter(Element element)
        {
            BoundingBoxXYZ bounding = element.get_BoundingBox(null);
            return (bounding.Max + bounding.Min) / 2;
        }

        public Room GetRoomByPoint(Document doc, XYZ point)
        {
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            collector.OfCategory(BuiltInCategory.OST_Rooms);
            foreach(Element e in collector)
            {
                Room room = e as Room;
                if (room != null)
                {
                    if (room.IsPointInRoom(point))
                    {
                        return room;
                    }
                }
            }
            return null;
        }
    }

    public class GroupPickFilter : ISelectionFilter
    //создаем фильтр
    {
        public bool AllowElement(Element elem)
        {
            if (elem.Category.Id.IntegerValue == (int)BuiltInCategory.OST_IOSModelGroups)
                return true;
            else
                return false;
        }

        public bool AllowReference(Reference reference, XYZ position)
        {
            return false;
        }
    }
}
