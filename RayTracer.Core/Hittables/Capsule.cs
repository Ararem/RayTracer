using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using static System.MathF;
using static System.Numerics.Vector3;

namespace RayTracer.Core.Hittables;

/// <summary>
///  A capsule shape, defined by two points, and a radius
/// </summary>
/// <param name="P1">The first point that makes up the capsule</param>
/// <param name="P2">The seconds point that makes up the capsule</param>
/// <param name="Radius">The radius of the capsule</param>
public record Capsule(Vector3 P1, Vector3 P2, float Radius) : Hittable
{
	private readonly Lazy<Vector3> centre = new(() => Lerp(P1, P2, 0.5f)); //Halfway between P1 and P2

	/// <inheritdoc/>
	[SuppressMessage("ReSharper", "IdentifierTypo")]
	public override HitRecord? TryHit(Ray ray, float kMin, float kMax, int depth)
	{
		/*
		 * I'm not sure how this code actually works, but I believe that it works by finding the distance between the line defined by the ray,
		 * and the line of the points P1 & P2. Then I would assume it solves the quadratic for when the distance == radius, and returns that as K.
		 */
		float   k  = -1f;
		Vector3 ba = P2         - P1;
		Vector3 oa = ray.Origin - P1;

		float baba = Dot(ba,            ba);
		float bard = Dot(ba,            ray.Direction);
		float baoa = Dot(ba,            oa);
		float rdoa = Dot(ray.Direction, oa);
		float oaoa = Dot(oa,            oa);

		float a = baba - (bard * bard);
		float b = (baba        * rdoa) - (baoa * bard);
		float c = (baba        * oaoa) - (baoa * baoa) - (Radius * Radius * baba);
		float h = (b           * b)    - (a    * c);
		if (h >= 0.0)
		{
			float t = (-b - Sqrt(h)) / a;
			float y = baoa + (t * bard);
			// body
			if ((y > 0.0) && (y < baba))
			{
				k = t;
			}
			// caps
			else
			{
				Vector3 oc = y <= 0.0 ? oa : ray.Origin - P2;
				b = Dot(ray.Direction, oc);
				c = Dot(oc, oc) - (Radius * Radius);
				h = (b                    * b) - c;
				if (h > 0.0) k = -b - Sqrt(h);
			}
		}

		//If we didn't hit anything `k` will still have it's value of -1, indicating nothing was hit
		//Not sure if it is also somehow set to negative values elsewhere when it's invalid, but I'm assuming it's fine
		// ReSharper disable once CompareOfFloatsByEqualityOperator
		if ((k != -1f) && (k >= kMin) && (k <= kMax))
		{
			Vector3 worldPos  = ray.PointAt(k);
			Vector3 localPos  = worldPos - centre.Value;
			Vector3 outNormal = CapNormal(worldPos, P1, P2, Radius);
			Vector2 uv        = UV(worldPos);
			bool    inside    = Dot(ray.Direction, outNormal) > 0f; //If the ray is 'inside' the sphere

			return new HitRecord(ray, worldPos, localPos, outNormal, k, !inside, uv);
		}

		return null;
	}

	/// <summary>
	///  Gets the UV coordinate for a point on the capsule's surface
	/// </summary>
	private Vector2 UV(Vector3 point)
	{
		Vector3 w = Normalize(P2 - P1);
		Vector3 u = Normalize(Cross(w, new Vector3(0, 0, 1)));
		Vector3 v = Normalize(Cross(u, w));
		// Original is:
		// Vector3 q = (point-Position1)*mat3(u, v, w);
		// Since C# doesn't have native 3x3 matrices, I've transposed it myself
		// One weird thing is `mat3(u,v,w)` should use u,v,w as the columns, but this doesn't quite work properly
		// When I expand out the matrix multiplication
		// So I had to use it as the rows to get it to work
		Vector3 t = point - P1;
		// Vector3 q = new(
		// 		(u.X * t.X) + (v.X* t.Y) + (w.X * t.Z),
		// 		(u.Y * t.X) + (v.Y* t.Y) + (w.Y * t.Z),
		// 		(u.Z * t.X) + (v.Z* t.Y) + (w.Z * t.Z)
		// );
		Vector3 q = new(
				(u.X * t.X) + (u.Y * t.Y) + (u.Z * t.Z),
				(v.X * t.X) + (v.Y * t.Y) + (v.Z * t.Z),
				(w.X * t.X) + (w.Y * t.Y) + (w.Z * t.Z)
		);
		Vector2 uv = new(Atan2(q.Y, q.X) /*Atan(q.Y / q.X)*/, q.Z);
		return uv;
	}

	// compute normal
	private static Vector3 CapNormal(Vector3 pos, Vector3 a, Vector3 b, float r)
	{
		Vector3 ba = b   - a;
		Vector3 pa = pos - a;
		float   h  = Math.Clamp(Dot(pa, ba) / Dot(ba, ba), 0f, 1f);
		return (pa - (h * ba)) / r;
	}
}

#region Original ShaderToy Code

/*
// The MIT License
// Copyright © 2016 Inigo Quilez
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions: The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software. THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.


// Intersection of a ray and a capped cylinder oriented in an arbitrary direction. There's
// only one sphere involved, not two.


// Other capsule functions:
//
// Capsule intersection: https://www.shadertoy.com/view/Xt3SzX
// Capsule bounding box: https://www.shadertoy.com/view/3s2SRV
// Capsule distance:     https://www.shadertoy.com/view/Xds3zN
// Capsule occlusion:    https://www.shadertoy.com/view/llGyzG

// List of ray-surface intersectors at https://www.shadertoy.com/playlist/l3dXRf
// and http://iquilezles.org/www/articles/intersectors/intersectors.htm



// intersect capsule : http://www.iquilezles.org/www/articles/intersectors/intersectors.htm
float capIntersect( in vec3 ro, in vec3 rd, in vec3 pa, in vec3 pb, in float r )
{
vec3  ba = pb - pa;
vec3  oa = ro - pa;

float baba = dot(ba,ba);
float bard = dot(ba,rd);
float baoa = dot(ba,oa);
float rdoa = dot(rd,oa);
float oaoa = dot(oa,oa);

float a = baba      - bard*bard;
float b = baba*rdoa - baoa*bard;
float c = baba*oaoa - baoa*baoa - r*r*baba;
float h = b*b - a*c;
if( h>=0.0 )
{
	float t = (-b-sqrt(h))/a;
	float y = baoa + t*bard;
	// body
	if( y>0.0 && y<baba ) return t;
	// caps
	vec3 oc = (y<=0.0) ? oa : ro - pb;
	b = dot(rd,oc);
	c = dot(oc,oc) - r*r;
	h = b*b - c;
	if( h>0.0 ) return -b - sqrt(h);
}
return -1.0;
}

// compute normal
vec3 capNormal( in vec3 pos, in vec3 a, in vec3 b, in float r )
{
vec3  ba = b - a;
vec3  pa = pos - a;
float h = clamp(dot(pa,ba)/dot(ba,ba),0.0,1.0);
return (pa - h*ba)/r;
}


// fake occlusion
float capOcclusion( in vec3 p, in vec3 n, in vec3 a, in vec3 b, in float r )
{
vec3  ba = b - a, pa = p - a;
float h = clamp(dot(pa,ba)/dot(ba,ba),0.0,1.0);
vec3  d = pa - h*ba;
float l = length(d);
float o = 1.0 - max(0.0,dot(-d,n))*r*r/(l*l*l);
return sqrt(o*o*o);
}

vec3 pattern( in vec2 uv )
{
vec3 col = vec3(0.6);
col += 0.4*smoothstep(-0.01,0.01,cos(uv.x*0.5)*cos(uv.y*0.5));
col *= smoothstep(-1.0,-0.98,cos(uv.x))*smoothstep(-1.0,-0.98,cos(uv.y));
return col;
}

#define AA 3

void mainImage( out vec4 fragColor, in vec2 fragCoord )
{
// camera movement
float an = 0.5*iTime;
vec3 ro = vec3( 1.0*cos(an), 0.4, 1.0*sin(an) );
vec3 ta = vec3( 0.0, 0.0, 0.0 );
// camera matrix
vec3 ww = normalize( ta - ro );
vec3 uu = normalize( cross(ww,vec3(0.0,1.0,0.0) ) );
vec3 vv = normalize( cross(uu,ww));


vec3 tot = vec3(0.0);

#if AA>1
for( int m=0; m<AA; m++ )
for( int n=0; n<AA; n++ )
{
	// pixel coordinates
	vec2 o = vec2(float(m),float(n)) / float(AA) - 0.5;
	vec2 p = (-iResolution.xy + 2.0*(fragCoord+o))/iResolution.y;
	#else
	vec2 p = (-iResolution.xy + 2.0*fragCoord)/iResolution.y;
	#endif

	// create view ray
	vec3 rd = normalize( p.x*uu + p.y*vv + 1.5*ww );

	const vec3  capA = vec3(-0.3,-0.1,-0.1);
	const vec3  capB = vec3(0.3,0.1,0.4);
	const float capR = 0.2;

	vec3 col = vec3(0.08)*(1.0-0.3*length(p)) + 0.02*rd.y;

	float t = capIntersect( ro, rd, capA, capB, capR );
	if( t>0.0 )
	{
		vec3  pos = ro + t*rd;
		vec3  nor = capNormal(pos, capA, capB, capR );
		vec3  lig = normalize(vec3(0.7,0.6,0.3));
		vec3  hal = normalize(-rd+lig);
		float dif = clamp( dot(nor,lig), 0.0, 1.0 );
		float amb = clamp( 0.5 + 0.5*dot(nor,vec3(0.0,1.0,0.0)), 0.0, 1.0 );
		float occ = 0.5 + 0.5*nor.y;

		vec3 w = normalize(capB-capA);
		vec3 u = normalize(cross(w,vec3(0,0,1)));
		vec3 v = normalize(cross(u,w) );
		vec3 q = (pos-capA)*mat3(u,v,w);
		col = pattern( vec2(12.0,64.0)*vec2(atan(q.y,q.x),q.z) );


		col *= vec3(0.2,0.3,0.4)*amb*occ + vec3(1.0,0.9,0.7)*dif;
		col += 0.4*pow(clamp(dot(hal,nor),0.0,1.0),12.0)*dif;
	}

	col = sqrt( col );

	tot += col;
#if AA>1
}
tot /= float(AA*AA);
#endif

// dither to remove banding in the background
tot += fract(sin(fragCoord.x*vec3(13,1,11)+fragCoord.y*vec3(1,7,5))*158.391832)/255.0;


fragColor = vec4( tot, 1.0 );
}
*/

#endregion