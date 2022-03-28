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
                GroupPickFilter groupPickFilter = new GroupPickFilter();
                Reference reference = uiDoc.Selection.PickObject(ObjectType.Element, groupPickFilter, "Выберите группу объектов");
                Element element = doc.GetElement(reference);
                Group group = element as Group;
                XYZ groupCentr = GetElementCenter(element);
                Room room = GetRoomByPoin(doc, groupCentr);
                XYZ roomCentr = GetElementCenter(room);
                XYZ offset = groupCentr - roomCentr;

                RoomPickFilter roomPickFilter = new RoomPickFilter();
                IList<Reference> rooms = uiDoc.Selection.PickObjects(ObjectType.Element, roomPickFilter,
                "Выберите комнаты, в которые требуется скопировать группу мебели");

                XYZ point = uiDoc.Selection.PickPoint("Выберите точку");

                Transaction transaction = new Transaction(doc);
                transaction.Start("Копирование группы объектов");

                PlaceFurnitureInRooms(doc, rooms, group.GroupType, offset);

                transaction.Commit();
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {

                return Result.Cancelled;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }

            return Result.Succeeded;
        }

        public XYZ GetElementCenter(Element element)
        {
            BoundingBoxXYZ bounding = element.get_BoundingBox(null);
            return (bounding.Max + bounding.Min) / 2;
        }
        public Room GetRoomByPoin(Document doc, XYZ poin)
        {
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            collector.OfCategory(BuiltInCategory.OST_Rooms);
            foreach (Element e in collector)
            {
                Room room = e as Room;
                if (room != null)
                {
                    if (room.IsPointInRoom(poin))
                    {
                        return room;
                    }
                }
            }
            return null;
        }
        public void PlaceFurnitureInRooms(Document doc, IList<Reference> rooms, GroupType gt, XYZ offsetXY)
        {

            //XYZ offsetXY = new XYZ(offset.X, offset.Y, 0);
            foreach (Reference r in rooms)
            {
                Room roomTarget = doc.GetElement(r) as Room;
                if (roomTarget != null)
                {
                    XYZ roomCenter = GetElementCenter(roomTarget);
                    Group group = doc.Create.PlaceGroup(roomCenter + offsetXY, gt);
                }
            }
        }

        public class GroupPickFilter : ISelectionFilter
        {
            public bool AllowElement(Element elem)
            {
                if (elem.Category.Id.IntegerValue == (int)BuiltInCategory.OST_IOSModelGroups)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }

            public bool AllowReference(Reference reference, XYZ position)
            {
                return false;
            }
        }
        public class RoomPickFilter : ISelectionFilter
        {
            public bool AllowElement(Element e)
            {
                return (e.Category.Id.IntegerValue.Equals(
                (int)BuiltInCategory.OST_Rooms));
            }
            public bool AllowReference(Reference r, XYZ p)
            {
                return false;
            }
        }
    }
}
