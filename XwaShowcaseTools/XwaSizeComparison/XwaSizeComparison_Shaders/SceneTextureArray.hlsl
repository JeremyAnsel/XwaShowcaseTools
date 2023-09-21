
Texture2DArray g_textures[100] : register(t10);

SamplerState g_sampler : register(s0);

float4 GetTextureArrayValue(uint c, float2 tex, uint k)
{
    if (c == 0xffffffff)
    {
        return (float4) 0;
    }

    uint c_i = c / 1024;
    uint c_j = c % 1024;

    float3 location = float3(tex, c_j * 2 + k);
    float4 color = 0;
    
    if (c_i < 50)
    {
        if (c_i < 25)
        {
            if (c_i < 12)
            {
                if (c_i < 6)
                {
                    if (c_i == 0)
                        color = g_textures[0].Sample(g_sampler, location);
                    if (c_i == 1)
                        color = g_textures[1].Sample(g_sampler, location);
                    if (c_i == 2)
                        color = g_textures[2].Sample(g_sampler, location);
                    if (c_i == 3)
                        color = g_textures[3].Sample(g_sampler, location);
                    if (c_i == 4)
                        color = g_textures[4].Sample(g_sampler, location);
                    if (c_i == 5)
                        color = g_textures[5].Sample(g_sampler, location);
                }
                else
                {
                    if (c_i == 6)
                        color = g_textures[6].Sample(g_sampler, location);
                    if (c_i == 7)
                        color = g_textures[7].Sample(g_sampler, location);
                    if (c_i == 8)
                        color = g_textures[8].Sample(g_sampler, location);
                    if (c_i == 9)
                        color = g_textures[9].Sample(g_sampler, location);
                    if (c_i == 10)
                        color = g_textures[10].Sample(g_sampler, location);
                    if (c_i == 11)
                        color = g_textures[11].Sample(g_sampler, location);
                }
            }
            else
            {
                if (c_i < 18)
                {
                    if (c_i == 12)
                        color = g_textures[12].Sample(g_sampler, location);
                    if (c_i == 13)
                        color = g_textures[13].Sample(g_sampler, location);
                    if (c_i == 14)
                        color = g_textures[14].Sample(g_sampler, location);
                    if (c_i == 15)
                        color = g_textures[15].Sample(g_sampler, location);
                    if (c_i == 16)
                        color = g_textures[16].Sample(g_sampler, location);
                    if (c_i == 17)
                        color = g_textures[17].Sample(g_sampler, location);
                }
                else
                {
                    if (c_i == 18)
                        color = g_textures[18].Sample(g_sampler, location);
                    if (c_i == 19)
                        color = g_textures[19].Sample(g_sampler, location);
                    if (c_i == 20)
                        color = g_textures[20].Sample(g_sampler, location);
                    if (c_i == 21)
                        color = g_textures[21].Sample(g_sampler, location);
                    if (c_i == 22)
                        color = g_textures[22].Sample(g_sampler, location);
                    if (c_i == 23)
                        color = g_textures[23].Sample(g_sampler, location);
                    if (c_i == 24)
                        color = g_textures[24].Sample(g_sampler, location);
                }
            }
        }
        else
        {
            if (c_i < 37)
            {
                if (c_i < 31)
                {
                    if (c_i == 25)
                        color = g_textures[25].Sample(g_sampler, location);
                    if (c_i == 26)
                        color = g_textures[26].Sample(g_sampler, location);
                    if (c_i == 27)
                        color = g_textures[27].Sample(g_sampler, location);
                    if (c_i == 28)
                        color = g_textures[28].Sample(g_sampler, location);
                    if (c_i == 29)
                        color = g_textures[29].Sample(g_sampler, location);
                    if (c_i == 30)
                        color = g_textures[30].Sample(g_sampler, location);
                }
                else
                {
                    if (c_i == 31)
                        color = g_textures[31].Sample(g_sampler, location);
                    if (c_i == 32)
                        color = g_textures[32].Sample(g_sampler, location);
                    if (c_i == 33)
                        color = g_textures[33].Sample(g_sampler, location);
                    if (c_i == 34)
                        color = g_textures[34].Sample(g_sampler, location);
                    if (c_i == 35)
                        color = g_textures[35].Sample(g_sampler, location);
                    if (c_i == 36)
                        color = g_textures[36].Sample(g_sampler, location);
                }
            }
            else
            {
                if (c_i < 43)
                {
                    if (c_i == 37)
                        color = g_textures[37].Sample(g_sampler, location);
                    if (c_i == 38)
                        color = g_textures[38].Sample(g_sampler, location);
                    if (c_i == 39)
                        color = g_textures[39].Sample(g_sampler, location);
                    if (c_i == 40)
                        color = g_textures[40].Sample(g_sampler, location);
                    if (c_i == 41)
                        color = g_textures[41].Sample(g_sampler, location);
                    if (c_i == 42)
                        color = g_textures[42].Sample(g_sampler, location);
                }
                else
                {
                    if (c_i == 43)
                        color = g_textures[43].Sample(g_sampler, location);
                    if (c_i == 44)
                        color = g_textures[44].Sample(g_sampler, location);
                    if (c_i == 45)
                        color = g_textures[45].Sample(g_sampler, location);
                    if (c_i == 46)
                        color = g_textures[46].Sample(g_sampler, location);
                    if (c_i == 47)
                        color = g_textures[47].Sample(g_sampler, location);
                    if (c_i == 48)
                        color = g_textures[48].Sample(g_sampler, location);
                    if (c_i == 49)
                        color = g_textures[49].Sample(g_sampler, location);
                }
            }
        }
    }
    else
    {
        if (c_i < 75)
        {
            if (c_i < 62)
            {
                if (c_i < 56)
                {
                    if (c_i == 50)
                        color = g_textures[50].Sample(g_sampler, location);
                    if (c_i == 51)
                        color = g_textures[51].Sample(g_sampler, location);
                    if (c_i == 52)
                        color = g_textures[52].Sample(g_sampler, location);
                    if (c_i == 53)
                        color = g_textures[53].Sample(g_sampler, location);
                    if (c_i == 54)
                        color = g_textures[54].Sample(g_sampler, location);
                    if (c_i == 55)
                        color = g_textures[55].Sample(g_sampler, location);
                }
                else
                {
                    if (c_i == 56)
                        color = g_textures[56].Sample(g_sampler, location);
                    if (c_i == 57)
                        color = g_textures[57].Sample(g_sampler, location);
                    if (c_i == 58)
                        color = g_textures[58].Sample(g_sampler, location);
                    if (c_i == 59)
                        color = g_textures[59].Sample(g_sampler, location);
                    if (c_i == 60)
                        color = g_textures[60].Sample(g_sampler, location);
                    if (c_i == 61)
                        color = g_textures[61].Sample(g_sampler, location);
                }
            }
            else
            {
                if (c_i < 68)
                {
                    if (c_i == 62)
                        color = g_textures[62].Sample(g_sampler, location);
                    if (c_i == 63)
                        color = g_textures[63].Sample(g_sampler, location);
                    if (c_i == 64)
                        color = g_textures[64].Sample(g_sampler, location);
                    if (c_i == 65)
                        color = g_textures[65].Sample(g_sampler, location);
                    if (c_i == 66)
                        color = g_textures[66].Sample(g_sampler, location);
                    if (c_i == 67)
                        color = g_textures[67].Sample(g_sampler, location);
                }
                else
                {
                    if (c_i == 68)
                        color = g_textures[68].Sample(g_sampler, location);
                    if (c_i == 69)
                        color = g_textures[69].Sample(g_sampler, location);
                    if (c_i == 70)
                        color = g_textures[70].Sample(g_sampler, location);
                    if (c_i == 71)
                        color = g_textures[71].Sample(g_sampler, location);
                    if (c_i == 72)
                        color = g_textures[72].Sample(g_sampler, location);
                    if (c_i == 73)
                        color = g_textures[73].Sample(g_sampler, location);
                    if (c_i == 74)
                        color = g_textures[74].Sample(g_sampler, location);
                }
            }
        }
        else
        {
            if (c_i < 87)
            {
                if (c_i < 81)
                {
                    if (c_i == 75)
                        color = g_textures[75].Sample(g_sampler, location);
                    if (c_i == 76)
                        color = g_textures[76].Sample(g_sampler, location);
                    if (c_i == 77)
                        color = g_textures[77].Sample(g_sampler, location);
                    if (c_i == 78)
                        color = g_textures[78].Sample(g_sampler, location);
                    if (c_i == 79)
                        color = g_textures[79].Sample(g_sampler, location);
                    if (c_i == 80)
                        color = g_textures[80].Sample(g_sampler, location);
                }
                else
                {
                    if (c_i == 81)
                        color = g_textures[81].Sample(g_sampler, location);
                    if (c_i == 82)
                        color = g_textures[82].Sample(g_sampler, location);
                    if (c_i == 83)
                        color = g_textures[83].Sample(g_sampler, location);
                    if (c_i == 84)
                        color = g_textures[84].Sample(g_sampler, location);
                    if (c_i == 85)
                        color = g_textures[85].Sample(g_sampler, location);
                    if (c_i == 86)
                        color = g_textures[86].Sample(g_sampler, location);
                }
            }
            else
            {
                if (c_i < 93)
                {
                    if (c_i == 87)
                        color = g_textures[87].Sample(g_sampler, location);
                    if (c_i == 88)
                        color = g_textures[88].Sample(g_sampler, location);
                    if (c_i == 89)
                        color = g_textures[89].Sample(g_sampler, location);
                    if (c_i == 90)
                        color = g_textures[90].Sample(g_sampler, location);
                    if (c_i == 91)
                        color = g_textures[91].Sample(g_sampler, location);
                    if (c_i == 92)
                        color = g_textures[92].Sample(g_sampler, location);
                }
                else
                {
                    if (c_i == 93)
                        color = g_textures[93].Sample(g_sampler, location);
                    if (c_i == 94)
                        color = g_textures[94].Sample(g_sampler, location);
                    if (c_i == 95)
                        color = g_textures[95].Sample(g_sampler, location);
                    if (c_i == 96)
                        color = g_textures[96].Sample(g_sampler, location);
                    if (c_i == 97)
                        color = g_textures[97].Sample(g_sampler, location);
                    if (c_i == 98)
                        color = g_textures[98].Sample(g_sampler, location);
                    if (c_i == 99)
                        color = g_textures[99].Sample(g_sampler, location);
                }
            }
        }
    }
    
    return color;
}
