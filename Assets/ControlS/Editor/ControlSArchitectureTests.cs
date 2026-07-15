#if UNITY_EDITOR
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace ControlS.Editor.Tests
{
    public sealed class ControlSArchitectureTests
    {
        [Test]
        public void Answer_SequenceIsDefensivelyCopied()
        {
            var source = new[] { 3, 1, 4, 2 };
            var answer = Answer.From(source);
            source[0] = 9;
            Assert.That(answer.TryGetIntSequence(out var stored), Is.True);
            Assert.That(stored, Is.EqualTo(new[] { 3, 1, 4, 2 }));
            stored[1] = 9;
            answer.TryGetIntSequence(out var reread);
            Assert.That(reread, Is.EqualTo(new[] { 3, 1, 4, 2 }));
        }

        [Test]
        public void StringMatcher_SupportsCurrentPuzzleNormalization()
        {
            var time = new StringAnswerMatcher("0217", true, true);
            var file = new StringAnswerMatcher("SAVE_S.tmp", true, false, true);
            Assert.That(time.IsMatch(Answer.From(" 02:17 ")), Is.True);
            Assert.That(file.IsMatch(Answer.From(" save_s.tmp ")), Is.True);
        }

        [Test]
        public void FloatAndSequenceMatchers_ValidateCurrentPuzzleShapes()
        {
            var slider = new FloatAnswerMatcher(FloatComparisonMode.GreaterOrEqual, .72f);
            var sequence = new IntSequenceAnswerMatcher(new[] { 3, 1, 4, 2 });
            Assert.That(slider.IsMatch(Answer.From(.8f)), Is.True);
            Assert.That(slider.IsMatch(Answer.From(.5f)), Is.False);
            Assert.That(sequence.IsMatch(Answer.From(new[] { 3, 1, 4, 2 })), Is.True);
            Assert.That(sequence.IsMatch(Answer.From(new[] { 1, 3, 4, 2 })), Is.False);
        }

        [Test]
        public void SampleScene_HasValidSerializedArchitecture()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity", OpenSceneMode.Additive);
            var valid = ControlSSceneValidation.Validate(scene, out var report);
            EditorSceneManager.CloseScene(scene, true);
            Assert.That(valid, Is.True, report);
        }
    }
}
#endif
