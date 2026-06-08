using System;
using System.IO;
using System.Text.Json;
using Last_Hope;
using Last_Hope.Engine;
using Last_Hope.SkillTree;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Last_Hope.UI;

/// <summary>
/// Builds <see cref="SkillTreeMenuCanvas"/> for the in-run skill overlay toggled from
/// <see cref="Menu.UpdateRunningMenu"/> (N key). Keeps JSON path resolution and theme wiring out of <see cref="Menu"/>.
/// </summary>
internal static class SkillTreeOverlayFactory
{
    private static readonly string WarriorTreeRelativePath = Path.Combine("SkillTree", "WarriorSkillTree.json");
    private static readonly string ArcherSkillTreeRelativePath = Path.Combine("SkillTree", "ArcherSkillTree.json");


    /// <summary>
    /// Loads warrior tree JSON, applies save state, hooks <see cref="Warrior"/> callbacks, returns canvas or throws if data is invalid.
    /// </summary>
    public static SkillTreeMenuCanvas CreateWarriorOverlay(GameManager gm, in Viewport viewport)
    {
        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        string projectRoot = Path.GetFullPath(Path.Combine(baseDir, "..", "..", ".."));
        string jsonPath = Path.Combine(projectRoot, WarriorTreeRelativePath);

        if (!File.Exists(jsonPath))
            jsonPath = Path.Combine(baseDir, WarriorTreeRelativePath);

        ClassSkillTreeData? treeData = null;
        if (File.Exists(jsonPath))
        {
            string rawJson = File.ReadAllText(jsonPath);
            var options = new JsonSerializerOptions
            {
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            };
            treeData = JsonSerializer.Deserialize<ClassSkillTreeData>(rawJson, options);
        }

        if (treeData == null)
            throw new InvalidOperationException($"[SkillTree] Could not load JSON at: {Path.GetFullPath(jsonPath)}. Set Copy to Output Directory if needed.");

        if (treeData.Nodes == null || treeData.Nodes.Count == 0)
            throw new InvalidOperationException("[SkillTree] JSON loaded but Nodes is empty.");

        SkillTreeState state = SkillTreeSaveManager.Load("Warrior");
        BaseSkillTree tree = new BaseSkillTree(treeData, state);

        if (gm._player is Warrior warrior)
        {
            tree.OnEffectApplied += warrior.ApplyNodeEffect;
            tree.OnTreeRespec += warrior.RevertAllSkillStats;
        }

        tree.RecalculateStats();

        var theme = new UIThemeData
        {
            LockedDesaturation = new Color(80, 85, 95),
            AccentGlowColor = new Color(255, 70, 20),
            BorderColor = new Color(160, 170, 180)
        };

        return new SkillTreeMenuCanvas(tree, theme, gm.Pixel, viewport);
    }

    /// <summary>
    /// Loads archer tree JSON, applies save state, hooks <see cref="Archer"/> callbacks, returns canvas or throws if data is invalid.
    /// </summary>
    public static SkillTreeMenuCanvas CreateArcherOverlay(GameManager gm, in Viewport viewport)
    {
        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        string projectRoot = Path.GetFullPath(Path.Combine(baseDir, "..", "..", ".."));
        string jsonPath = Path.Combine(projectRoot, ArcherSkillTreeRelativePath);

        if (!File.Exists(jsonPath))
            jsonPath = Path.Combine(baseDir, ArcherSkillTreeRelativePath);

        ClassSkillTreeData? treeData = null;
        if (File.Exists(jsonPath))
        {
            string rawJson = File.ReadAllText(jsonPath);
            var options = new JsonSerializerOptions
            {
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            };
            treeData = JsonSerializer.Deserialize<ClassSkillTreeData>(rawJson, options);
        }

        if (treeData == null)
            throw new InvalidOperationException($"[SkillTree] Could not load JSON at: {Path.GetFullPath(jsonPath)}. Set Copy to Output Directory if needed.");

        if (treeData.Nodes == null || treeData.Nodes.Count == 0)
            throw new InvalidOperationException("[SkillTree] JSON loaded but Nodes is empty.");

        SkillTreeState state = SkillTreeSaveManager.Load("Archer");
        BaseSkillTree tree = new BaseSkillTree(treeData, state);

        if (gm._player is Archer archer)
        {
            tree.OnEffectApplied += archer.ApplyNodeEffect;
            tree.OnTreeRespec += archer.RevertAllSkillStats;
        }

        tree.RecalculateStats();

        var theme = new UIThemeData
        {
            LockedDesaturation = new Color(80, 85, 95),
            AccentGlowColor = new Color(255, 70, 20),
            BorderColor = new Color(160, 170, 180)
        };

        return new SkillTreeMenuCanvas(tree, theme, gm.Pixel, viewport);
    }
}
