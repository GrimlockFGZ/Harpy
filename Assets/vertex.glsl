#version 330 core
layout (location = 0) in vec3 aPos;

out vec2 v_vTexcoord;
uniform vec2 uOffsets[5];
uniform float uTime;

void main()
{
    vec2 basePos = uOffsets[gl_InstanceID];
    float pulse =1.0 + sin(uTime * 2.0) *0.2;
    vec2 animatedPos = basePos *pulse;
    float angle = uTime;
    mat2 rot = mat2(cos(angle), -sin(angle),sin(angle),cos(angle));
    vec2 rotateShape = rot*aPos.xy;
   

    gl_Position = vec4(rotateShape + animatedPos,
    aPos.z, 1.0);
}
