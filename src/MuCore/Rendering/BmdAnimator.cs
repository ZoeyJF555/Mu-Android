using System.Numerics;
using Mu.Core.Formats.Bmd;

namespace Mu.Core.Rendering;

/// <summary>
/// Calcula la pose de un modelo BMD en un instante dado. Recorre la jerarquía
/// de huesos y, para cada uno, interpola entre las dos claves que rodean al
/// instante. El resultado son las matrices de mundo que usa el skinning.
/// </summary>
public sealed class BmdAnimator
{
    private readonly BmdModel _model;
    private readonly Matrix4x4[] _world;
    private readonly Matrix4x4[] _local;

    public BmdAnimator(BmdModel model)
    {
        _model = model;
        _world = new Matrix4x4[model.Bones.Count];
        _local = new Matrix4x4[model.Bones.Count];
    }

    /// <summary>Número de acciones (animaciones) del modelo.</summary>
    public int ActionCount => _model.Actions.Count;

    /// <summary>Número de fotogramas de una acción.</summary>
    public int KeyCount(int action) =>
        action >= 0 && action < _model.Actions.Count ? _model.Actions[action].AnimationKeys : 0;

    /// <summary>
    /// Calcula la pose de la acción <paramref name="action"/> en el fotograma
    /// <paramref name="frame"/> (puede ser decimal, se interpola).
    /// </summary>
    public SkeletonPose Pose(int action, float frame)
    {
        if (_model.Bones.Count == 0)
        {
            return new SkeletonPose { BoneMatrices = [] };
        }

        if (action < 0 || action >= _model.Actions.Count)
        {
            action = 0;
        }

        var keys = _model.Actions[action].AnimationKeys;
        var pose = new Matrix4x4[_model.Bones.Count];

        for (var b = 0; b < _model.Bones.Count; b++)
        {
            var bone = _model.Bones[b];
            _local[b] = bone.IsDummy || bone.Tracks.Count == 0
                ? Matrix4x4.Identity
                : LocalTransform(bone.Tracks[action], keys, frame);

            var parent = bone.Parent;
            _world[b] = parent >= 0 && parent < b
                ? _local[b] * _world[parent]
                : _local[b];

            pose[b] = _world[b];
        }

        return new SkeletonPose { BoneMatrices = pose };
    }

    private static Matrix4x4 LocalTransform(BmdBoneTrack track, int keys, float frame)
    {
        if (keys <= 0 || track.Positions.Count == 0)
        {
            return Matrix4x4.Identity;
        }

        if (keys == 1)
        {
            return Skeleton.LocalTransform(track.Positions[0], track.Rotations[0]);
        }

        var wrapped = frame % keys;
        if (wrapped < 0)
        {
            wrapped += keys;
        }

        var index = (int)wrapped;
        var next = (index + 1) % keys;
        var amount = wrapped - index;

        var position = Vector3.Lerp(track.Positions[index], track.Positions[next], amount);
        var rotation = Skeleton.Interpolate(
            Skeleton.AngleToQuaternion(track.Rotations[index]),
            Skeleton.AngleToQuaternion(track.Rotations[next]),
            amount);

        return Matrix4x4.CreateFromQuaternion(rotation) * Matrix4x4.CreateTranslation(position);
    }
}
