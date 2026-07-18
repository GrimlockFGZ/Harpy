#version 330 core
layout (location = 0) in vec3 aPos;

out vec2 v_vTexcoord;

uniform mat4 uModel;
uniform mat4 uView;
uniform mat4 uProjection;

uniform int uInstanceCount;
uniform float uRadius;
uniform float uTime;

void main()
{
    // Arrange instances around a ring in the XZ plane, in world space.
    float count = float(max(uInstanceCount, 1));
    float angle = float(gl_InstanceID) * 6.28318530718 / count;
    float pulse = 1.0 + sin(uTime * 2.0) * 0.2;
    vec3 ringOffset = vec3(cos(angle), 0.0, sin(angle)) * uRadius * pulse;

    // Spin each triangle in place within its own local XY plane.
    float rotAngle = uTime;
    mat2 rot = mat2(cos(rotAngle), -sin(rotAngle), sin(rotAngle), cos(rotAngle));
    vec2 rotatedXY = rot * aPos.xy;
    vec3 localPos = vec3(rotatedXY, aPos.z);

    vec4 worldPos = uModel * vec4(localPos + ringOffset, 1.0);
    gl_Position = uProjection * uView * worldPos;

    v_vTexcoord = aPos.xy;
}
