namespace Last_Hope.Collision
{

    public class LinePieceCollider : Collider, IEquatable<LinePieceCollider>
    {

        public Vector2 Start;
        public Vector2 End;

        /// <summary>
        /// The length of the LinePiece, changing the length moves the end vector to adjust the length.
        /// </summary>
        public float Length 
        { 
            get { 
                return (End - Start).Length(); 
            } 
            set {
                End = Start + GetDirection() * value; 
            }
        }

        /// <summary>
        /// The A component from the standard line formula Ax + By + C = 0
        /// </summary>
        public float StandardA
        {
            // A = Py - Qy
            get
            {
                var a = Start.Y - End.Y;
                return a;
            }
        }

        /// <summary>
        /// The B component from the standard line formula Ax + By + C = 0
        /// </summary>
        public float StandardB
        {
            // B = Qx - Px
            get
            {
                var b = End.X - Start.X;
                return b;
            }
        }

        /// <summary>
        /// The C component from the standard line formula Ax + By + C = 0
        /// </summary>
        public float StandardC
        {
            // C = Px X Qy - Py X Qx
            get
            {
                var c = Start.X * End.Y - Start.Y * End.X;
                return c;
            }
        }

        public LinePieceCollider(Vector2 start, Vector2 end)
        {
            Start = start;
            End = end;
        }
        
        public LinePieceCollider(Vector2 start, Vector2 direction, float length)
        {
            Start = start;
            End = start + direction * length;
        }

        /// <summary>
        /// Should return the angle between a given direction and the up vector.
        /// </summary>
        /// <param name="direction">The Vector2 pointing out from (0,0) to calculate the angle to.</param>
        /// <returns> The angle in radians between the the up vector and the direction to the cursor.</returns>
        public static float GetAngle(Vector2 direction)
        {
            var angle = (float)Math.Atan2(direction.X, - (direction.Y));
            return angle;
        }


        /// <summary>
        /// Calculates the normalized vector pointing from point1 to point2
        /// </summary>
        /// <returns> A Vector2 containing the direction from point1 to point2. </returns>
        public static Vector2 GetDirection(Vector2 point1, Vector2 point2)
        {
            var direction = point2 - point1;
            var normalizedDirection = Vector2.Normalize(direction);
            return normalizedDirection;
        }


        /// <summary>
        /// Gets whether or not the Line intersects another Line
        /// </summary>
        /// <param name="other">The Line to check for intersection</param>
        /// <returns>true there is any overlap between the line and the Line.</returns>
        public override bool Intersects(LinePieceCollider other)
        {
            //https://stackoverflow.com/questions/4543506/algorithm-for-intersection-of-2-lines
            var intersectPoint = GetIntersection(other);
            float delta = this.StandardA * other.StandardB - other.StandardA * this.StandardB;
            if (delta == 0)
            {
                return false;
            }

            if (this.Contains(intersectPoint) && other.Contains(intersectPoint))
            {
                return true;
            }
            return false;
        }


        /// <summary>
        /// Gets whether or not the line intersects a Circle.
        /// </summary>
        /// <param name="other">The Circle to check for intersection.</param>
        /// <returns>true there is any overlap between the two Circles.</returns>
        public override bool Intersects(CircleCollider other)
        {
            var nearestPoint = NearestPointOnLine(other.Center);
            if (other.Contains(nearestPoint)) 
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Gets whether or not the Line intersects the Rectangle.
        /// </summary>
        /// <param name="other">The Rectangle to check for intersection.</param>
        /// <returns>true there is any overlap between the Line and the Rectangle.</returns>
        public override bool Intersects(RectangleCollider other)
        {
            var A = new Vector2(other.shape.Left, other.shape.Top);
            var B = new Vector2(other.shape.Right, other.shape.Top);
            var C = new Vector2(other.shape.Right, other.shape.Bottom);
            var D = new Vector2(other.shape.Left, other.shape.Bottom);

            var AB = new LinePieceCollider(A, B);
            var AD = new LinePieceCollider(A, D);
            var BC = new LinePieceCollider(B, C);
            var CD = new LinePieceCollider(C, D);

            if (this.Intersects(AB) || this.Intersects(AD) || this.Intersects(BC) || this.Intersects(CD) || other.Contains(Start) || other.Contains(End))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Calculates the intersection point between 2 lines.
        /// </summary>
        /// <param name="Other">The line to intersect with</param>
        /// <returns>A Vector2 with the point of intersection.</returns>
        public Vector2 GetIntersection(LinePieceCollider Other)
        {
            float delta = this.StandardA * Other.StandardB - Other.StandardA * this.StandardB;
            if (delta == 0)
            {
                return Vector2.Zero;
            }
            float x = (Other.StandardB * this.StandardC - this.StandardB * Other.StandardC) / delta;
            float y = (this.StandardA * Other.StandardC - Other.StandardA * this.StandardC) / delta;
            Vector2 result = new Vector2(x, y);
            return result;
        }

        /// <summary>
        /// Finds the nearest point on a line to a given vector, taking into account if the line is .
        /// </summary>
        /// <param name="other">The Vector you want to find the nearest point to.</param>
        /// <returns>The nearest point on the line.</returns>
        public Vector2 NearestPointOnLine(Vector2 other)
        {
            //https://stackoverflow.com/questions/3120357/get-closest-point-to-a-line
            var AP = other - Start;
            var AB = End - Start;

            var magnitudeAB = AB.LengthSquared();
            var ABAPproduct = Vector2.Dot(AP, AB);
            var distance = ABAPproduct / magnitudeAB;
            if (distance < 0)
            {
                return Start;
            }
            else if (distance > 1)
            {
                return End;
            }
            else
            {
                return Start + AB * distance;
            }    
        }

        /// <summary>
        /// Returns the enclosing Axis Aligned Bounding Box containing the control points for the line.
        /// As an unbound line has infinite length, the returned bounding box assumes the line to be bound.
        /// </summary>
        /// <returns></returns>
        public override Rectangle GetBoundingBox()
        {
            Point topLeft = new Point((int)Math.Min(Start.X, End.X), (int)Math.Min(Start.Y, End.Y));
            Point size = new Point((int)Math.Max(Start.X, End.X), (int)Math.Max(Start.Y, End.Y)) - topLeft;
            return new Rectangle(topLeft,size);
        }


        /// <summary>
        /// Gets whether or not the provided coordinates lie on the line.
        /// </summary>
        /// <param name="coordinates">The coordinates to check.</param>
        /// <returns>true if the coordinates are on the line.</returns>
        public override bool Contains(Vector2 coordinates)
        {
            var onTheLine = (coordinates.X >= this.Start.X && coordinates.X <= this.End.X || coordinates.X >= this.End.X && coordinates.X <= this.Start.X) 
                            && (coordinates.Y >= this.Start.Y && coordinates.Y <= this.End.Y || coordinates.Y >= this.End.Y && coordinates.Y <= this.Start.Y);
            return onTheLine;
        }

        public bool Equals(LinePieceCollider other)
        {
            return other.Start == this.Start && other.End == this.End;
        }

        /// <summary>
        /// Calculates the normalized vector pointing from point1 to point2
        /// </summary>
        /// <returns> A Vector2 containing the direction from point1 to point2. </returns>
        public static Vector2 GetDirection(Point point1, Point point2)
        {
            return GetDirection(point1.ToVector2(), point2.ToVector2());
        }


        /// <summary>
        /// Calculates the normalized vector pointing from point1 to point2
        /// </summary>
        /// <returns> A Vector2 containing the direction from point1 to point2. </returns>
        public Vector2 GetDirection()
        {
            return GetDirection(Start, End);
        }


        /// <summary>
        /// Should return the angle between a given direction and the up vector.
        /// </summary>
        /// <param name="direction">The Vector2 pointing out from (0,0) to calculate the angle to.</param>
        /// <returns> The angle in radians between the the up vector and the direction to the cursor.</returns>
        public float GetAngle()
        {
            return GetAngle(GetDirection());
        }
    }
}
