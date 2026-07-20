#version 330 core
layout (location = 0) in uint aPacked;

uniform mat4 uModel;
uniform mat4 uView;
uniform mat4 uProjection;

out vec3 vNormal;
flat out uint vBlockId;
out float vLight;

const vec3 kNormals[6] = vec3[6](
    vec3( 1.0, 0.0, 0.0), vec3(-1.0, 0.0, 0.0),
    vec3( 0.0, 1.0, 0.0), vec3( 0.0,-1.0, 0.0),
    vec3( 0.0, 0.0, 1.0), vec3( 0.0, 0.0,-1.0)
);

void main()
{
    uint x  =  aPacked        & 0x3Fu;
    uint y  = (aPacked >> 6)  & 0x3Fu;
    uint z  = (aPacked >> 12) & 0x3Fu;
    uint n  = (aPacked >> 18) & 0x7u;
    uint id = (aPacked >> 21) & 0x7Fu;
    uint lt = (aPacked >> 28) & 0xFu;

    vec3 localPos = vec3(float(x), float(y), float(z));

    vNormal  = normalize(mat3(transpose(inverse(uModel))) * kNormals[n]);
    vBlockId = id;
    vLight   = float(lt) / 15.0;

    gl_Position = uProjection * uView * uModel * vec4(localPos, 1.0);
}