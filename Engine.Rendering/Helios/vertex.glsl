#version 330 core
layout (location = 0) in vec3 aPos;

uniform vec3 uOffsets[100]; // Array to hold positions of your entities
uniform float uGlobalTime;

void main()
{
    // Get the specific offset for this instance (Entity 1, Entity 2, etc.)
    vec3 instanceOffset = uOffsets[gl_InstanceID];
    
    // Add a little bit of that movement pulse you liked
    float movement = sin(uGlobalTime + float(gl_InstanceID)) * 0.1;

    gl_Position = vec4(aPos.x + instanceOffset.x + movement, 
                       aPos.y + instanceOffset.y, 
                       aPos.z + instanceOffset.z, 1.0);
}