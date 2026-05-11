#version 330 core
layout (location = 0) in vec3 aPos;

out vec2 v_vTexcoord;
uniform int uInstanceCount;
uniform float uRadius;
uniform float uTime;

void main()
{
    float count = float(max(uInstanceCount, 1));
    float angle = float(gl_InstanceID) * 6.28318530718 / count;
    vec2 basePos = vec2(cos(angle), sin(angle)) * uRadius;
    float pulse =1.0 + sin(uTime * 2.0) *0.2;
    vec2 animatedPos = basePos *pulse;
    float rotAngle = uTime;
    mat2 rot = mat2(cos(rotAngle), -sin(rotAngle),sin(rotAngle),cos(rotAngle));
    vec2 rotateShape = rot*aPos.xy;
   

    gl_Position = vec4(rotateShape + animatedPos,
    aPos.z, 1.0);
}
