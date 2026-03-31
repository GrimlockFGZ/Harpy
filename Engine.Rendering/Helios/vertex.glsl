#version 330 core
layout (location = 0) in vec3 aPos;

uniform vec2 uOffsets[3];
uniform float uGlobalTime;

void main()
{
    vec2 offset = uOffsets[gl_InstanceID];
    float movement = sin(uGlobalTime + float(gl_InstanceID)) * 0.2;

    gl_Position = vec4(aPos.x + offset.x + movement,
    aPos.y + offset.y,
    aPos.z, 1.0);
}
