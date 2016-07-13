attribute vec4 position;
attribute vec2 texCoordIn;

uniform mat4 projection;

varying vec2 texCoord;

void main()
{
    texCoord = texCoordIn;
	gl_Position = projection * position;
}
