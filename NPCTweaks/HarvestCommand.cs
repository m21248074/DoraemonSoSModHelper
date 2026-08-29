using Define;

namespace NPCTweaks
{
    public class HarvestCommand : AbstractCommand
    {
        public override int GetButton()
        {
            return 41;
        }

        public override string GetCommandName()
        {
            return SingletonMonoBehaviour<MasterManager>.Instance.TextMaster.GetText(1712070001);
        }

        public override Scene.Farm.StateEnum GetState()
        {
            return Scene.Farm.StateEnum.UI;
        }

        public override bool IsValid()
        {
            return true;
        }

        public override bool Execute(out ResponseModel response)
        {
            response = null;
            bool result;
            if (!IsValid())
                return false;
            else if (!DoWork())
                result = false;
            else
            {
                OutputMsg();
                result = true;
            }
            return result;
        }

        private static bool DoWork()
        {
            UserModel user = SingletonMonoBehaviour<UserManager>.Instance.User;
            bool result;
            if (user == null)
                result = false;
            else
            {
                MapModel map = user.Farm.GetMap(Map.ID_FARM);
                if (map == null)
                    result = false;
                else
                {
                    map.WetGrounds();
                    for (int i = 0; i < map.GroundAreas.Length; i++)
                    {
                        for (int j = 0; j < map.GroundAreas[i].Grounds.Length; j++)
                        {
                            var ground = map.GroundAreas[i].Grounds[j];
                            if (ground.HasCrop && ground.Crop.CanHarvest)
                            {
                                CropModel crop = map.GroundAreas[i].Grounds[j].Crop;
                                user.ShippingBox.AddItem(new ItemModel(crop.Master.ItemId, crop.Quality, 1, -1));
                                map.GroundAreas[i].Grounds[j].HarvestCrop();
                            }
                        }
                    }
                    result = true;
                }
            }
            return result;
        }
    
        private static void OutputMsg()
        {
            FarmTopUIController farmTopUIController = SingletonMonoBehaviour<UIManager>.Instance.GetUIController(UI.TypeEnum.FarmTop) as FarmTopUIController;
            farmTopUIController.AddCharacterLogRequest(SingletonMonoBehaviour<MasterManager>.Instance.TextMaster.GetText(Work.Korobokkur.GetAssistLogTextId(Character.Korobokkur.AssistType.CropWork)), 201041010);
        }
    }
}
