using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StardewValleyCoop
{
	public class DisplayInformation
	{
		public string id;
		public string name;
		public int posX;
		public int posY;
		public int width;
		public int height;

		public DisplayInformation(string id, string name, int posX, int posY, int width, int height)
		{
			this.id = id;
            this.name = name;
            this.posX = posX;
			this.posY = posY;
			this.width = width;
			this.height = height;
		}

		public List<InstanceWindowPositionSettings> DivideDisplayBetweenPlayers(int numPlayers)
		{
			List<InstanceWindowPositionSettings> instanceWindowPositionSettingsList = new();
			int width = this.width;
			int height = this.height;
			int posX = 0;
			int posY = 0;

			InstanceWindowPositionSettings previousInstance = new();
			instanceWindowPositionSettingsList.Add(previousInstance);
			previousInstance.displayName = name;
			previousInstance.posX = posX;
			previousInstance.posY = posY;
			previousInstance.width = width;
			previousInstance.height = height;

			for (int i = 0; i < numPlayers - 1; i++)
			{
				previousInstance.displayName = name;
				previousInstance.posX = posX;
				previousInstance.posY = posY;
				previousInstance.width = width;
				previousInstance.height = height;

				if (i % 2 == 0)
				{
					height /= 2;
					posY = height;
				}
				else
				{
					width /= 2;
					posX = width;
				}

				previousInstance.width = width;
				previousInstance.height = height;

				previousInstance = new();

				previousInstance.displayName = name;
				previousInstance.width = width;
				previousInstance.height = height;
				previousInstance.posX = posX;
				previousInstance.posY = posY;
				instanceWindowPositionSettingsList.Add(previousInstance);
			}

			return instanceWindowPositionSettingsList;
		}
	}
}
