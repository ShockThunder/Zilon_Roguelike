﻿using System;
using System.Collections.Generic;
using System.Linq;

using JetBrains.Annotations;
using Zilon.Core.Tactics.Spatial;

namespace Zilon.Core.MapGenerators.RoomStyle
{
    public abstract class FixRoomGeneratorRandomSourceBase
    {
        protected readonly List<Tuple<OffsetCoords, OffsetCoords>> Connections;

        protected FixRoomGeneratorRandomSourceBase()
        {
            // 20 комнат - это 6х6 матрица
            Connections = new List<Tuple<OffsetCoords, OffsetCoords>>(20);
        }

        /// <summary>
        /// Выбирает комнаты, с которыми есть соединение.
        /// </summary>
        /// <param name="currentRoom">Текущая комната, для которой ищуются соединённые соседи.</param>
        /// <param name="maxNeighbors">Максимальное количество соединённых соседей.</param>
        /// <param name="availableRooms">Набор доступных для соединения соседенй.</param>
        /// <returns>
        /// Возвращает целевые комнаты для соединения.
        /// </returns>
        [NotNull, ItemNotNull]
        public Room[] RollConnectedRooms(Room currentRoom, int maxNeighbors, IList<Room> availableRooms)
        {
            if (!availableRooms.Any())
            {
                return new Room[0];
            }

            var currentConnection = Connections.Single(x =>
                        x.Item1.X == currentRoom.PositionX &&
                        x.Item1.Y == currentRoom.PositionY);

            var connectedRoom = availableRooms.Single(x =>
                x.PositionX == currentConnection.Item2.X &&
                x.PositionY == currentConnection.Item2.Y);

            return new[] { connectedRoom };
        }

        /// <summary>
        /// Выбрасывает случаный набор элементов интерьера комнаты.
        /// </summary>
        /// <param name="roomWidth">Ширина комнаты.</param>
        /// <param name="roomHeight">Высота комнаты.</param>
        /// <returns> Возвращает набор элементов интерьера комнаты. </returns>
        public RoomInteriorObjectMeta[] RollInteriorObjects(int roomWidth, int roomHeight)
        {
            return new RoomInteriorObjectMeta[0];
        }

        /// <summary>
        /// Возвращает матрицу смежности между комнатами (сеть комнат).
        /// </summary>
        /// <param name="rooms">Всё комнаты, которые должны быть соединены в сеть.</param>
        /// <param name="maxNeighbors">Максимальное количество соседей у комнаты.</param>
        /// <returns>
        /// Возвращает словарь, представляющий собой матрицу смежности комнат.
        /// Минимальное число соседей - 1. Максимальное - не превышает указанное в аргументе значение.
        /// </returns>
        public IDictionary<Room, Room[]> RollRoomNet(IEnumerable<Room> rooms, int maxNeighbors)
        {
            var result = new Dictionary<Room, Room[]>();

            foreach (var currentRoom in rooms)
            {
                var currentConnection = Connections.SingleOrDefault(x =>
                        x.Item1.X == currentRoom.PositionX &&
                        x.Item1.Y == currentRoom.PositionY);

                if (currentConnection == null)
                {
                    continue;
                }

                var connectedRoom = rooms.SingleOrDefault(x =>
                    x.PositionX == currentConnection.Item2.X &&
                    x.PositionY == currentConnection.Item2.Y);

                if (connectedRoom != null)
                {
                    result.Add(currentRoom, new[] { connectedRoom });
                }
            }

            return result;
        }

        public Size[] RollRoomSize(int minSize, int maxSize, int count)
        {
            var sizeList = new Size[count];
            for (var i = 0; i < count; i++)
            {
                sizeList[i] = RollRoomSize(minSize, maxSize);
            }

            return sizeList;
        }

        public HexNode RollTransitionNode(IEnumerable<HexNode> openRoomNodes)
        {
            return openRoomNodes.First();
        }

        public IEnumerable<RoomTransition> RollTransitions(IEnumerable<RoomTransition> openTransitions)
        {
            return new[] { openTransitions.First() };
        }

        /// <summary>
        /// Выбрасывает случайный размер комнаты.
        /// </summary>
        /// <param name="minSize">Минимальный размер комнаты.</param>
        /// <param name="maxSize">Максимальный размер комнаты.</param>
        /// <returns>
        /// Возвращает размер с произвольными шириной и высотой в диапазоне (minSize, maxSize).
        /// </returns>
        /// <remarks>
        /// Источник рандома возвращает случайный размер комнаты в указанном диапазоне.
        /// </remarks>
        protected abstract Size RollRoomSize(int minSize, int maxSize);
    }
}