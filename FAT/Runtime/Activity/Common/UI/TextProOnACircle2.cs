//MIT License

//Copyright(c) 2019 Antony Vitillo(a.k.a. "Skarredghost")

//Permission is hereby granted, free of charge, to any person obtaining a copy
//of this software and associated documentation files (the "Software"), to deal
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//copies of the Software, and to permit persons to whom the Software is
//furnished to do so, subject to the following conditions:

//The above copyright notice and this permission notice shall be included in all
//copies or substantial portions of the Software.

//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
//SOFTWARE.

using UnityEngine;
using System.Collections;
using TMPro;

namespace TMPro
{
    /// <summary>
    /// Class for drawing a Text Pro text following a circle arc
    /// </summary>
    [ExecuteInEditMode]
    public class TextProOnACircle2 : TextProOnACurve
    {
        /// <summary>
        /// The radius of the text circle arc
        /// </summary>
        [SerializeField]
        [Tooltip("The radius of the text circle arc")]
        private float m_radius = 10.0f;

        /// <summary>
        /// How much degrees the text arc should span
        /// </summary>
        [SerializeField]
        [Tooltip("How much degrees the text arc should span")]
        private float m_arcDegrees = 0.0f;

        /// <summary>
        /// The angular offset at which the arc should be centered, in degrees.
        /// -90 degrees means that the text is centered on the heighest point
        /// </summary>
        [SerializeField]
        [Tooltip("The angular offset at which the arc should be centered, in degrees")]
        private float m_angularOffset = -90;

        /// <summary>
        /// How many maximum degrees per letters should be. For instance, if you specify
        /// 10 degrees, the distance between the letters will never be superior to 10 degrees.
        /// It is useful to create text that gracefully expands until it reaches the full arc,
        /// without making the letters to sparse when the string is short
        /// </summary>
        [SerializeField]
        [Tooltip("The maximum angular distance between letters, in degrees")]
        private int m_maxDegreesPerLetter = 360;
        public float yOffset;

        /// <summary>
        /// Computes the transformation matrix that maps the offsets of the vertices of each single character from
        /// the character's center to the final destinations of the vertices so that the text follows a curve
        /// </summary>
        /// <param name="charMidBaselinePosfloat">Position of the central point of the character</param>
        /// <param name="zeroToOnePos">Horizontal position of the character relative to the bounds of the box, in a range [0, 1]</param>
        /// <param name="textInfo">Information on the text that we are showing</param>
        /// <param name="charIdx">Index of the character we have to compute the transformation for</param>
        /// <returns>Transformation matrix to be applied to all vertices of the text</returns>
        protected override Matrix4x4 ComputeTransformationMatrix(Vector3 charMidBaselinePos, float zeroToOnePos, Bounds textRenderBounds, TMP_TextInfo textInfo, int charIdx)      
        {
            //calculate the actual degrees of the arc considering the maximum distance between letters
            //float actualArcDegrees = Mathf.Min(m_arcDegrees, textInfo.characterCount / textInfo.lineCount * m_maxDegreesPerLetter);

            var actualArcDegrees = 360 * textRenderBounds.size.x / (2 * Mathf.PI * m_radius) + m_arcDegrees;

            //compute the angle at which to show this character.
            //We want the string to be centered at the top point of the circle, so we first convert the position from a range [0, 1]
            //to a [-0.5, 0.5] one and then add m_angularOffset degrees, to make it centered on the desired point
            var angle = ((zeroToOnePos - 0.5f) * actualArcDegrees + m_angularOffset) * Mathf.Deg2Rad; //we need radians for sin and cos

            //compute the coordinates of the new position of the central point of the character. Use sin and cos since we are on a circle.
            //Notice that we have to do some extra calculations because we have to take in count that text may be on multiple lines
            var x0 = Mathf.Cos(angle);
            var y0 = Mathf.Sin(angle);
            
            // 修复多行居中问题：正确计算包含行距的偏移
            var currentLineNumber = textInfo.characterInfo[charIdx].lineNumber;
            var lineC = textInfo.lineCount;
            
            // 使用实际的行间距来计算偏移
            // 计算所有行的总高度（考虑行距）
            float totalHeight = 0f;
            if (lineC > 1)
            {
                // 使用第一行和最后一行的baseline差值来获取准确的总高度
                totalHeight = textInfo.lineInfo[0].baseline - textInfo.lineInfo[lineC - 1].baseline;
            }
            
            // 计算当前行相对于中心的偏移
            float rOffset = 0f;
            if (lineC > 1)
            {
                // 当前行的baseline相对于第一行的偏移
                var currentLineOffset = textInfo.lineInfo[0].baseline - textInfo.lineInfo[currentLineNumber].baseline;
                // 减去总高度的一半，使其居中
                rOffset = currentLineOffset - totalHeight * 0.5f;
            }
            
            var radiusForThisLine = m_radius + rOffset;
            var newMideBaselinePos = new Vector2(x0 * radiusForThisLine, -y0 * radiusForThisLine + yOffset - m_radius); //actual new position of the character

            //compute the trasformation matrix: move the points to the just found position, then rotate the character to fit the angle of the curve 
            //(-90 is because the text is already vertical, it is as if it were already rotated 90 degrees)
            return Matrix4x4.TRS(new Vector3(newMideBaselinePos.x, newMideBaselinePos.y, 0), Quaternion.AngleAxis(-Mathf.Atan2(y0, x0) * Mathf.Rad2Deg - 90, Vector3.forward), Vector3.one);
        }
    }
}
