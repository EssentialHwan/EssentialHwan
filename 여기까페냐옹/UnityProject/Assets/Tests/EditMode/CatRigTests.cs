using NUnit.Framework;
using YeogiCafe.Art;

namespace YeogiCafe.Tests
{
    public class CatRigTests
    {
        static readonly string[] Cats = { "cat_cheese", "cat_mackerel", "cat_gray", "cat_black", "cat_siamese", "cat_tuxedo", "cat_cow", "cat_calico" };

        static int Opaque(PixelCanvas c) { int n = 0; foreach (var p in c.Pixels) if (p.a > 0) n++; return n; }

        [Test]
        public void 모든_클립_프레임이_그려진다()
        {
            foreach (var id in Cats)
                foreach (var d in CatClips.All)
                    for (int i = 0; i < d.frames; i++)
                        Assert.Greater(Opaque(CatClips.Frame(id, d, i)), 300, $"{id} {d.name}{i}");
        }

        [Test]
        public void 걷기_프레임은_서로_다르다()
        {
            var d = CatClips.Find("walk");
            var a = CatClips.Frame("cat_cheese", d, 0).Pixels;
            var b = CatClips.Frame("cat_cheese", d, d.frames / 2).Pixels;
            int diff = 0; for (int i = 0; i < a.Length; i++) if (!a[i].Equals(b[i])) diff++;
            Assert.Greater(diff, 40);
        }

        [Test]
        public void 발은_땅선_아래로_그려지지_않는다()
        {
            foreach (var d in CatClips.All)
            {
                var c = CatClips.Frame("cat_tuxedo", d, 0);
                for (int y = 0; y < CatRig.Ground - 1; y++)
                    for (int x = 0; x < c.W; x++)
                    {
                        var p = c.Get(x, y);
                        Assert.IsTrue(p.a == 0 || p.a < 255, $"{d.name} ({x},{y})");
                    }
            }
        }
    }
}
