#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RoadCreatorPro
{
    [System.Serializable]
    public class IntersectionConnection : IComparable<IntersectionConnection>
    {
        public Vector3 leftTangent;
        public Vector3 rightTangent;
        public List<Lane> connectedLanes = new List<Lane>();
        public List<int> connectedLaneIndexes = new List<int>();
        public float YRotation;
        public float length;
        public Point roadPoint;
        public bool endConnection;

        public int startIndex = 0;
        public int endIndex = 1;
        public Vector3 leftPoint;
        public Vector3 rightPoint;

        // Lane turn markings
        public int turnMarkingsRepetitions = 3;
        public int turnMarkingsAmount = 1;
        public float turnMarkingsStartOffset = 1.3f;
        public float turnMarkingsContiniusOffset = 10f;
        public float turnMarkingsYOffset = 0;
        public List<Vector3Bool> turnMarkings = new List<Vector3Bool>();
        public bool sameXOffsetsForAllRepetitions = true;
        public List<FloatList> turnMarkingsXOffsets = new List<FloatList>();

        public IntersectionConnection(Point roadPoint, Vector3 leftTangent, Vector3 rightTangent, bool endConnection)
        {
            this.roadPoint = roadPoint;
            this.leftTangent = leftTangent;
            this.rightTangent = rightTangent;
            this.endConnection = endConnection;

            // Add default turn markings
            turnMarkings.Add(new Vector3Bool(true, true, false));

            for (int i = 0; i < 3; i++)
            {
                FloatList floatList = new FloatList();
                floatList.list.Add(1.5f);
                turnMarkingsXOffsets.Add(floatList);
            }
        }

        public int CompareTo(IntersectionConnection intersectionConnection)
        {
            if (intersectionConnection == null)
            {
                return 1;
            }
            else
            {
                return this.YRotation.CompareTo(intersectionConnection.YRotation);
            }
        }
    }
}
#endif