#version 330 core

layout (location = 0) in vec3 aPos;
layout (location = 1) in vec3 aNormal;
layout (location = 2) in float aBlockId;

uniform mat4 uModel;
uniform mat4 uView;
uniform mat4 uProjection;

out vec3 vNormal;
out float vBlockId;

void main()
{
    vNormal  = normalize(mat3(transpose(inverse(uModel))) * aNormal);
    vBlockId = aBlockId;
gl_Position = vec4(0.0, 0.0, 0.0, 1.0);}
